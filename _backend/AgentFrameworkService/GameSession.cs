using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafiaSimulation.AgentFrameworkService;

internal sealed class AgentRuntime : IAsyncDisposable
{
    public required AgentSpec Spec { get; init; }
    public required string ProviderName { get; init; }
    public required ChatClientAgent Agent { get; init; }
    public required AgentSession Session { get; init; }
    public required IChatClient ChatClient { get; init; }
    public required ChatClientAgentRunOptions RunOptions { get; init; }
    public bool IsAvailable { get; set; }
    public string UnavailableReason { get; set; } = "";

    public async ValueTask DisposeAsync()
    {
        if (ChatClient is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
        else if (ChatClient is IDisposable disposable) disposable.Dispose();
    }
}

internal sealed class OperationProgress
{
    private readonly object _sync = new();
    private readonly List<AgentAction> _actions = new();
    private bool _completed;

    public void Add(AgentAction action)
    {
        lock (_sync) _actions.Add(action);
    }

    public void Complete()
    {
        lock (_sync) _completed = true;
    }

    public OperationProgressResponse Snapshot(int offset)
    {
        lock (_sync)
        {
            int safeOffset = Math.Clamp(offset, 0, _actions.Count);
            return new OperationProgressResponse(
                _actions.Skip(safeOffset).ToList(),
                _actions.Count,
                _completed);
        }
    }
}

internal sealed class GameSession : IAsyncDisposable
{
    private readonly DeploymentOptions _deployment;
    private string DataDirectory => _deployment.DataDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _storageGate = new(1, 1);
    private readonly object _memorySync = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _operations = new();
    private readonly ConcurrentDictionary<string, OperationProgress> _operationProgress = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _dialogueHistory = new();
    private readonly Dictionary<string, List<GameEventRequest>> _events;
    private readonly Dictionary<string, List<string>> _transcripts;
    private int _eventCount;
    private readonly IReadOnlyCollection<ProviderRateLimitCoordinator> _providerRateLimiters;
    private readonly bool _ownsRateLimiters;

    private GameSession(
        string sessionId,
        string language,
        PromptSet prompts,
        Dictionary<string, AgentRuntime> agents,
        List<ProviderStatus> providerStatuses,
        IReadOnlyCollection<ProviderRateLimitCoordinator> providerRateLimiters,
        bool ownsRateLimiters,
        DeploymentOptions deployment)
    {
        _deployment = deployment;
        SessionId = sessionId;
        Language = language;
        Prompts = prompts;
        Agents = agents;
        ProviderStatuses = providerStatuses;
        _providerRateLimiters = providerRateLimiters;
        _ownsRateLimiters = ownsRateLimiters;
        _events = agents.Keys.ToDictionary(id => id, _ => new List<GameEventRequest>());
        _transcripts = agents.Keys.ToDictionary(id => id, _ => new List<string>());
    }

    public string SessionId { get; }
    public string Language { get; }
    public PromptSet Prompts { get; }
    public Dictionary<string, AgentRuntime> Agents { get; }
    public List<ProviderStatus> ProviderStatuses { get; }

    public static async Task<GameSession> CreateAsync(
        CreateSessionRequest request,
        DeploymentOptions deployment,
        ProviderRateLimitStore globalLimiters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId)) throw new ArgumentException("session_id is required.");
        if (request.Agents.Count == 0) throw new ArgumentException("At least one agent is required.");

        ProviderCollection collection = ProviderFactory.ReadCollection(request.ProviderConfigJson, deployment);
        Dictionary<string, ProviderConfig> providers = collection.Providers
            .ToDictionary(item => item.Provider, StringComparer.OrdinalIgnoreCase);
        var runtimes = new Dictionary<string, AgentRuntime>(StringComparer.Ordinal);
        var reachability = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var providerStatuses = new List<ProviderStatus>();
        bool ownsRateLimiters = deployment.LocalDevelopment && !string.IsNullOrWhiteSpace(request.ProviderConfigJson);
        Dictionary<string, ProviderRateLimitCoordinator> rateLimiters = providers.ToDictionary(
            item => item.Key,
            item => ownsRateLimiters
                ? new ProviderRateLimitCoordinator(item.Value)
                : globalLimiters.Get(item.Value),
            StringComparer.OrdinalIgnoreCase);
        var sessionBudget = new SessionCallBudget(deployment);
        try
        {
            foreach (AgentSpec spec in request.Agents)
            {
                string providerName = string.IsNullOrWhiteSpace(collection.ProviderOverride) ? spec.Provider : collection.ProviderOverride;
                if (!providers.TryGetValue(providerName, out ProviderConfig? provider))
                    throw new InvalidOperationException($"Provider configuration not found: {providerName}");

                if (!reachability.TryGetValue(providerName, out bool isAvailable))
                {
                    isAvailable = await ProviderFactory.CanReachAsync(provider, cancellationToken);
                    reachability[providerName] = isAvailable;
                    providerStatuses.Add(new ProviderStatus(
                        providerName,
                        isAvailable,
                        isAvailable
                            ? $"연결 가능 · {rateLimiters[providerName].Description}"
                            : $"외부 AI 서버에 연결할 수 없어 게임 내 대체 응답을 사용합니다. · {rateLimiters[providerName].Description}"));
                }

                IChatClient chatClient = ProviderFactory
                    .Create(provider, spec.ModelOverride, deployment)
                    .UseProviderRateLimits(rateLimiters[providerName], sessionBudget);
                ChatOptions chatOptions = ProviderFactory.BuildChatOptions(provider, deployment);
                chatOptions.Instructions = PromptFactory.BuildAgentInstructions(spec, request.Prompts, request.Language);
                var agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
                {
                    Id = spec.AgentId,
                    Name = SafeAgentName(spec.AgentId),
                    Description = $"Mafia game participant {spec.AgentName}",
                    ChatOptions = chatOptions
                });
                AgentSession agentSession = await agent.CreateSessionAsync(cancellationToken);
                runtimes.Add(spec.AgentId, new AgentRuntime
                {
                    Spec = spec,
                    ProviderName = providerName,
                    Agent = agent,
                    Session = agentSession,
                    ChatClient = chatClient,
                    RunOptions = new ChatClientAgentRunOptions(chatOptions),
                    IsAvailable = isAvailable,
                    UnavailableReason = isAvailable ? "" : "provider_preflight_failed"
                });
            }

            var session = new GameSession(
                request.SessionId,
                request.Language,
                request.Prompts,
                runtimes,
                providerStatuses,
                rateLimiters.Values,
                ownsRateLimiters,
                deployment);
            await session.ResetPersistentStateAsync(cancellationToken);
            return session;
        }
        catch
        {
            foreach (AgentRuntime runtime in runtimes.Values) await runtime.DisposeAsync();
            if (ownsRateLimiters)
                foreach (ProviderRateLimitCoordinator limiter in rateLimiters.Values) limiter.Dispose();
            throw;
        }
    }

    public async Task<AgentAction> RunAgentActionAsync(AgentActionRequest request, CancellationToken requestAborted)
    {
        await _gate.WaitAsync(requestAborted);
        try
        {
            if (request.ActionType == "talk_decision" && request.RecentPartner)
            {
                var rejected = new AgentAction
                {
                    ActorId = request.AgentId,
                    Action = "TALK_REJECT",
                    TargetId = request.TargetId,
                    Reasoning = "최근 대화 상대이므로 재대화를 거절함"
                };
                await LogResponseAsync(rejected, null, "talk_decision", requestAborted);
                return rejected;
            }

            AgentRuntime runtime = GetRuntime(request.AgentId);
            if (!runtime.IsAvailable)
                return await SaveFallbackAsync(
                    BuildFallbackAction(runtime.Spec.AgentId, request.ActionType, request.TargetId),
                    request.ActionType,
                    requestAborted);

            using CancellationTokenSource operation = BeginOperation(request.OperationId, requestAborted);
            try
            {
                try
                {
                    string task = PromptFactory.BuildActionTask(request, Prompts, MemoriesFor(request.AgentId));
                    AgentResponse<AgentAction> response = await runtime.Agent.RunAsync<AgentAction>(
                        task, runtime.Session, JsonDefaults.Options, runtime.RunOptions, operation.Token);
                    AgentAction action = response.Result ?? throw new InvalidOperationException("Agent Framework returned no AgentAction.");
                    action.ActorId = request.AgentId;
                    await LogResponseAsync(action, response, request.ActionType, operation.Token);
                    AddTranscript(action);
                    await SaveAsync(operation.Token);
                    return action;
                }
                catch (OperationCanceledException) when (operation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (IsProviderFailure(exception, requestAborted))
                {
                    MarkProviderUnavailable(runtime.ProviderName, exception);
                    return await SaveFallbackAsync(
                        BuildFallbackAction(runtime.Spec.AgentId, request.ActionType, request.TargetId),
                        request.ActionType,
                        requestAborted);
                }
            }
            finally
            {
                EndOperation(request.OperationId);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<List<AgentAction>> RunMeetingAsync(MeetingRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> speakerIds = request.SpeakerAgentIds ?? request.AliveAgentIds;
        return RunGroupChatAsync(
            request.OperationId,
            speakerIds,
            PromptFactory.BuildMeetingTask(Prompts, request.AliveAgentIds, request.CurrentDay, Math.Clamp(request.Rounds, 1, 4)),
            Math.Max(1, speakerIds.Count * Math.Clamp(request.Rounds, 1, 4)),
            "meeting",
            cancellationToken);
    }

    public async Task<List<AgentAction>> RunDialogueAsync(DialogueRequest request, CancellationToken cancellationToken)
    {
        if (request.ParticipantIds.Count != 2) throw new ArgumentException("Dialogue requires exactly two participants.");
        int minimumTurns = Math.Clamp(request.MinTurns, 2, 6);
        int maximumTurns = Math.Clamp(request.MaxTurns, minimumTurns, 6);
        List<AgentSpec> participantSpecs = request.ParticipantIds
            .Select(id => GetRuntime(id).Spec)
            .ToList();
        string pairKey = DialoguePairKey(request.ParticipantIds[0], request.ParticipantIds[1]);
        IReadOnlyList<string> previousDialogue = _dialogueHistory.TryGetValue(pairKey, out ConcurrentQueue<string>? history)
            ? history.ToArray().TakeLast(6).ToList()
            : [];
        List<AgentAction> actions = await RunGroupChatAsync(
            request.OperationId,
            request.ParticipantIds,
            PromptFactory.BuildDialogueTask(
                Prompts,
                participantSpecs,
                request.CurrentDay,
                minimumTurns,
                maximumTurns,
                previousDialogue),
            maximumTurns,
            "dialogue",
            cancellationToken,
            minimumTurns);
        foreach (AgentAction action in actions)
        {
            action.TargetId = request.ParticipantIds.FirstOrDefault(id => id != action.ActorId) ?? "";
        }
        return actions.Take(maximumTurns).ToList();
    }

    public async Task<List<AgentAction>> RunPhaseAsync(PhaseDecisionRequest request, string phase, CancellationToken requestAborted)
    {
        await _gate.WaitAsync(requestAborted);
        try
        {
            using CancellationTokenSource operation = BeginOperation(request.OperationId, requestAborted);
            try
            {
                var actions = new List<AgentAction>();
                IReadOnlyList<string> actingAgentIds = request.ActingAgentIds ?? request.AliveAgentIds;
                foreach (string agentId in actingAgentIds)
                {
                    AgentRuntime runtime = GetRuntime(agentId);
                    if (phase == "night" && !runtime.Spec.IsMafia) continue;
                    if (!runtime.IsAvailable)
                    {
                        AgentAction fallback = BuildFallbackAction(agentId, phase, "");
                        actions.Add(fallback);
                        AddTranscript(fallback);
                        await LogResponseAsync(fallback, null, phase, requestAborted);
                        continue;
                    }
                    string prompt = phase == "voting" ? Prompts.Voting : Prompts.Night;
                    string task = PromptFactory.BuildPhaseTask(Prompts, prompt, phase, agentId, request.AliveAgentIds, request.CurrentDay, MemoriesFor(agentId));
                    try
                    {
                        AgentResponse<AgentAction> response = await runtime.Agent.RunAsync<AgentAction>(
                            task, runtime.Session, JsonDefaults.Options, runtime.RunOptions, operation.Token);
                        AgentAction action = response.Result ?? throw new InvalidOperationException("Agent Framework returned no AgentAction.");
                        action.ActorId = agentId;
                        actions.Add(action);
                        AddTranscript(action);
                        await LogResponseAsync(action, response, phase, operation.Token);
                    }
                    catch (OperationCanceledException) when (operation.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception) when (IsProviderFailure(exception, requestAborted))
                    {
                        MarkProviderUnavailable(runtime.ProviderName, exception);
                        AgentAction fallback = BuildFallbackAction(agentId, phase, "");
                        actions.Add(fallback);
                        AddTranscript(fallback);
                        await LogResponseAsync(fallback, null, phase, requestAborted);
                    }
                }
                await SaveAsync(operation.Token);
                return actions;
            }
            finally
            {
                EndOperation(request.OperationId);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordEventAsync(GameEventRequest gameEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Increment(ref _eventCount) > 2_000)
        {
            Interlocked.Decrement(ref _eventCount);
            throw new InvalidOperationException("This game has reached its event limit.");
        }
        lock (_memorySync)
        {
            IEnumerable<string> recipients = gameEvent.Visibility == "private"
                ? new[] { gameEvent.OwnerAgentId }.Where(Agents.ContainsKey)
                : Agents.Keys;
            foreach (string id in recipients) _events[id].Add(gameEvent);
        }
        await SaveAsync(cancellationToken);
    }

    public bool Cancel(string operationId)
    {
        if (!_operations.TryGetValue(operationId, out CancellationTokenSource? source)) return false;
        source.Cancel();
        return true;
    }

    public OperationProgressResponse GetOperationProgress(string operationId, int offset)
    {
        if (!_operationProgress.TryGetValue(operationId, out OperationProgress? progress))
            throw new KeyNotFoundException($"Unknown operation: {operationId}");
        return progress.Snapshot(offset);
    }

    public SessionState ExportState()
    {
        lock (_memorySync)
        {
            return new SessionState(
                _events.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()),
                _transcripts.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()));
        }
    }

    public async Task LoadStateAsync(SessionState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_memorySync)
        {
            foreach (string id in Agents.Keys)
            {
                _events[id] = state.Events.GetValueOrDefault(id)?.ToList() ?? new();
                _transcripts[id] = state.Transcripts.GetValueOrDefault(id)?.ToList() ?? new();
            }
        }
        await SaveAsync(cancellationToken);
    }

    private async Task<List<AgentAction>> RunGroupChatAsync(
        string operationId,
        IReadOnlyList<string> participantIds,
        string task,
        int maximumIterations,
        string responseType,
        CancellationToken requestAborted,
        int minimumIterations = 0)
    {
        if (participantIds.Count == 0) throw new ArgumentException("At least one group-chat participant is required.");

        var progress = new OperationProgress();
        _operationProgress[operationId] = progress;
        using CancellationTokenSource operation = BeginOperation(operationId, requestAborted);
        try
        {
            var actions = new List<AgentAction>();
            List<AgentRuntime> requestedRuntimes = participantIds
                .Distinct(StringComparer.Ordinal)
                .Select(GetRuntime)
                .ToList();
            List<AgentRuntime> unavailableRuntimes = requestedRuntimes.Where(runtime => !runtime.IsAvailable).ToList();

            if (responseType == "dialogue" && unavailableRuntimes.Count > 0)
            {
                AgentRuntime unavailable = unavailableRuntimes[0];
                string targetId = participantIds.FirstOrDefault(id => id != unavailable.Spec.AgentId) ?? "";
                AgentAction fallback = BuildFallbackAction(unavailable.Spec.AgentId, responseType, targetId);
                await AppendGroupActionAsync(fallback, null, responseType, actions, progress, operation.Token);
                await SaveAsync(operation.Token);
                return actions;
            }

            foreach (AgentRuntime unavailable in unavailableRuntimes)
            {
                AgentAction fallback = BuildFallbackAction(unavailable.Spec.AgentId, responseType, "");
                await AppendGroupActionAsync(fallback, null, responseType, actions, progress, operation.Token);
            }

            List<AgentRuntime> availableRuntimes = requestedRuntimes.Where(runtime => runtime.IsAvailable).ToList();
            if (availableRuntimes.Count == 0)
            {
                await SaveAsync(operation.Token);
                return actions;
            }

            var currentSceneHistory = new List<string>();
            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                operation.Token.ThrowIfCancellationRequested();
                AgentRuntime runtime = availableRuntimes[iteration % availableRuntimes.Count];
                AgentAction action;
                AgentResponse<AgentAction>? response = null;
                bool gateEntered = false;
                try
                {
                    await _gate.WaitAsync(operation.Token);
                    gateEntered = true;
                    if (!runtime.IsAvailable)
                    {
                        action = BuildFallbackAction(runtime.Spec.AgentId, responseType, "");
                    }
                    else
                    {
                        string turnTask = PromptFactory.BuildAgentTurnTask(
                            Prompts,
                            task,
                            runtime.Spec,
                            MemoriesFor(runtime.Spec.AgentId),
                            currentSceneHistory);
                        response = await runtime.Agent.RunAsync<AgentAction>(
                            turnTask,
                            runtime.Session,
                            JsonDefaults.Options,
                            runtime.RunOptions,
                            operation.Token);
                        action = response.Result ?? throw new InvalidOperationException("Agent Framework returned no AgentAction.");
                    }
                }
                catch (OperationCanceledException) when (operation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (IsProviderFailure(exception, requestAborted))
                {
                    MarkProviderUnavailable(runtime.ProviderName, exception);
                    string failedTargetId = responseType == "dialogue"
                        ? participantIds.FirstOrDefault(id => id != runtime.Spec.AgentId) ?? ""
                        : "";
                    action = BuildFallbackAction(runtime.Spec.AgentId, responseType, failedTargetId);
                }
                finally
                {
                    if (gateEntered) _gate.Release();
                }

                action.ActorId = runtime.Spec.AgentId;
                if (responseType == "dialogue")
                {
                    action.TargetId = participantIds.FirstOrDefault(id => id != action.ActorId) ?? "";
                    int completedTurns = actions.Count + 1;
                    if (completedTurns < minimumIterations) action.ContinueDialogue = true;
                    else if (completedTurns >= maximumIterations) action.ContinueDialogue = false;
                }

                await AppendGroupActionAsync(action, response, responseType, actions, progress, operation.Token);
                if (!string.IsNullOrWhiteSpace(action.DialogueContent))
                    currentSceneHistory.Add($"{runtime.Spec.AgentName}: {action.DialogueContent.Trim()}");

                if (responseType == "dialogue" &&
                    actions.Count >= minimumIterations &&
                    !action.ContinueDialogue) break;

                // Event-memory requests, including participant statements, can be accepted
                // between turns instead of waiting for the entire group operation to finish.
                await Task.Yield();
            }

            if (actions.Count == 0)
            {
                string actorId = participantIds[0];
                string targetId = responseType == "dialogue"
                    ? participantIds.FirstOrDefault(id => id != actorId) ?? ""
                    : "";
                AgentAction fallback = BuildFallbackAction(actorId, responseType, targetId);
                await AppendGroupActionAsync(fallback, null, responseType, actions, progress, operation.Token);
            }

            await SaveAsync(operation.Token);
            return actions;
        }
        finally
        {
            EndOperation(operationId);
            progress.Complete();
        }
    }

    private async Task AppendGroupActionAsync(
        AgentAction action,
        AgentResponse? response,
        string responseType,
        List<AgentAction> actions,
        OperationProgress progress,
        CancellationToken cancellationToken)
    {
        actions.Add(action);
        AddTranscript(action);
        if (responseType == "dialogue") RememberDialogueLine(action);
        await LogResponseAsync(action, response, responseType, cancellationToken);
        progress.Add(action);
    }

    private async Task<AgentAction> SaveFallbackAsync(
        AgentAction action,
        string responseType,
        CancellationToken cancellationToken)
    {
        AddTranscript(action);
        await LogResponseAsync(action, null, responseType, cancellationToken);
        await SaveAsync(cancellationToken);
        return action;
    }

    private static AgentAction BuildFallbackAction(string actorId, string actionType, string targetId)
    {
        bool isDialogue = actionType is "talk_decision" or "dialogue";
        return new AgentAction
        {
            ActorId = actorId,
            Action = actionType switch
            {
                "talk_decision" => "TALK_REJECT",
                "dialogue" or "meeting" => "TALK",
                _ => "NONE"
            },
            TargetId = targetId,
            DialogueContent = actionType switch
            {
                "talk_decision" or "dialogue" => "다른 일을 하느라 부르는 소리도 들리지 않아 보인다.",
                "meeting" => "생각을 정리하지 못한 듯 말없이 주변을 살핀다.",
                _ => ""
            },
            DialogueIntent = isDialogue ? "대화에 응하지 않음" : "",
            Reasoning = "지금은 행동을 이어갈 수 없는 상태",
            ContinueDialogue = false
        };
    }

    private void RememberDialogueLine(AgentAction action)
    {
        if (string.IsNullOrWhiteSpace(action.ActorId) ||
            string.IsNullOrWhiteSpace(action.TargetId) ||
            string.IsNullOrWhiteSpace(action.DialogueContent)) return;

        string pairKey = DialoguePairKey(action.ActorId, action.TargetId);
        ConcurrentQueue<string> history = _dialogueHistory.GetOrAdd(pairKey, _ => new ConcurrentQueue<string>());
        string speakerName = Agents.TryGetValue(action.ActorId, out AgentRuntime? runtime)
            ? runtime.Spec.AgentName
            : action.ActorId;
        history.Enqueue($"{speakerName}: {action.DialogueContent.Trim()}");
        while (history.Count > 6) history.TryDequeue(out _);
    }

    private static string DialoguePairKey(string firstId, string secondId)
        => string.Compare(firstId, secondId, StringComparison.Ordinal) <= 0
            ? $"{firstId}\u001f{secondId}"
            : $"{secondId}\u001f{firstId}";

    private void MarkProviderUnavailable(string providerName, Exception exception)
    {
        foreach (AgentRuntime runtime in Agents.Values.Where(runtime =>
                     runtime.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase)))
        {
            runtime.IsAvailable = false;
            runtime.UnavailableReason = exception.GetType().Name;
        }

        int index = ProviderStatuses.FindIndex(status =>
            status.Provider.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        var status = new ProviderStatus(
            providerName,
            false,
            "외부 AI 서버에 연결할 수 없어 게임 내 대체 응답을 사용합니다.");
        if (index >= 0) ProviderStatuses[index] = status;
        else ProviderStatuses.Add(status);
    }

    private static bool IsProviderFailure(Exception exception, CancellationToken requestAborted)
    {
        if (exception is OperationCanceledException)
            return !requestAborted.IsCancellationRequested;
        if (exception is HttpRequestException or System.Net.Sockets.SocketException or TimeoutException)
            return true;
        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(inner => IsProviderFailure(inner, requestAborted));
        if (exception.InnerException != null && IsProviderFailure(exception.InnerException, requestAborted))
            return true;

        string message = exception.Message.ToLowerInvariant();
        return message.Contains("retry failed") ||
               message.Contains("bad gateway") ||
               message.Contains("destination host") ||
               message.Contains("connection refused") ||
               message.Contains("socket") ||
               message.Contains("소켓") ||
               message.Contains("연결할 수") ||
               message.Contains("429") ||
               message.Contains("502") ||
               message.Contains("503");
    }

    private IReadOnlyList<GameEventRequest> MemoriesFor(string agentId)
    {
        lock (_memorySync)
            return _events.GetValueOrDefault(agentId)?.ToList() ?? [];
    }

    private void AddTranscript(AgentAction action)
    {
        lock (_memorySync)
        {
            if (!_transcripts.TryGetValue(action.ActorId, out List<string>? transcript)) return;
            transcript.Add(JsonSerializer.Serialize(action, JsonDefaults.Options));
            if (transcript.Count > 48) transcript.RemoveRange(0, transcript.Count - 48);
        }
    }

    private async Task LogResponseAsync(AgentAction action, AgentResponse? response, string responseType, CancellationToken cancellationToken)
    {
        if (!_deployment.LogResponses) return;
        Directory.CreateDirectory(DataDirectory);
        object record = new
        {
            timestamp = DateTimeOffset.UtcNow,
            session_id = SessionId,
            response_type = responseType,
            actor_id = action.ActorId,
            action = action.Action,
            target_id = action.TargetId,
            dialogue_content = action.DialogueContent,
            continue_dialogue = action.ContinueDialogue,
            input_tokens = response?.Usage?.InputTokenCount ?? 0,
            output_tokens = response?.Usage?.OutputTokenCount ?? 0,
            total_tokens = response?.Usage?.TotalTokenCount ?? 0
        };
        await File.AppendAllTextAsync(
            ResponseLogPath,
            JsonSerializer.Serialize(record, JsonDefaults.Options) + Environment.NewLine,
            cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!_deployment.PersistSessionState) return;
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(DataDirectory);
            string path = StatePath;
            string temporary = path + ".tmp";
            await File.WriteAllTextAsync(
                temporary,
                JsonSerializer.Serialize(ExportState(), JsonDefaults.Options),
                cancellationToken);
            File.Move(temporary, path, true);
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private string StatePath => Path.Combine(DataDirectory, $"{SafeAgentName(SessionId)}.json");
    private string ResponseLogPath => Path.Combine(DataDirectory, $"{SafeAgentName(SessionId)}.responses.jsonl");

    private async Task ResetPersistentStateAsync(CancellationToken cancellationToken)
    {
        if (!_deployment.PersistSessionState) return;
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.Delete(StatePath);
            await File.WriteAllTextAsync(
                StatePath,
                JsonSerializer.Serialize(ExportState(), JsonDefaults.Options),
                cancellationToken);
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private async Task DeletePersistentStateAsync()
    {
        if (!_deployment.PersistSessionState) return;
        await _storageGate.WaitAsync();
        try
        {
            File.Delete(StatePath);
            File.Delete(StatePath + ".tmp");
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private AgentRuntime GetRuntime(string agentId)
        => Agents.TryGetValue(agentId, out AgentRuntime? runtime) ? runtime : throw new KeyNotFoundException($"Unknown agent: {agentId}");

    private CancellationTokenSource BeginOperation(string operationId, CancellationToken requestAborted)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        if (!_operations.TryAdd(operationId, source))
        {
            source.Dispose();
            throw new InvalidOperationException($"Operation already exists: {operationId}");
        }
        return source;
    }

    private void EndOperation(string operationId) => _operations.TryRemove(operationId, out _);

    private static string SafeAgentName(string value)
    {
        string safe = new(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        if (string.IsNullOrWhiteSpace(safe)) safe = "agent";
        if (char.IsDigit(safe[0])) safe = "agent_" + safe;
        return safe[..Math.Min(safe.Length, 64)];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (CancellationTokenSource source in _operations.Values) source.Cancel();
        await _gate.WaitAsync();
        try
        {
            _operationProgress.Clear();
            foreach (AgentRuntime runtime in Agents.Values) await runtime.DisposeAsync();
            if (_ownsRateLimiters)
                foreach (ProviderRateLimitCoordinator limiter in _providerRateLimiters) limiter.Dispose();
            await DeletePersistentStateAsync();
        }
        finally
        {
            _gate.Release();
            _storageGate.Dispose();
            _gate.Dispose();
        }
    }
}
