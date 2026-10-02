using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.Extensions.AI;

namespace MafiaSimulation.AgentFrameworkService;

internal sealed record SessionUsageSnapshot(
    int Calls,
    long CountedTokens,
    long MeasuredTokens,
    int MeasuredCalls,
    int UnmeasuredCalls,
    int FailedCalls,
    int MaxCalls,
    int MaxTokens);

internal sealed class SessionAiLimitException(
    string reasonCode,
    SessionUsageSnapshot? usage = null) : InvalidOperationException
{
    public string ReasonCode { get; } = reasonCode;
    public SessionUsageSnapshot? Usage { get; } = usage;
}

internal sealed class ProviderTpmLimitException(string providerName) : InvalidOperationException
{
    public string ProviderName { get; } = providerName;
}

internal sealed class ProviderQueueLimitException(string providerName, string limitName) : InvalidOperationException
{
    public string ProviderName { get; } = providerName;
    public string LimitName { get; } = limitName;
}

/// <summary>
/// llm_config의 Provider별 RPM/TPM을 Agent Framework의 IChatClient 미들웨어에서 적용한다.
/// 동일 Provider를 사용하는 여러 NPC는 하나의 coordinator를 공유한다.
/// </summary>
internal sealed class ProviderRateLimitCoordinator : IDisposable
{
    private const int DefaultEstimatedOutputTokens = 256;
    private readonly RateLimiter? _requestLimiter;
    private readonly RateLimiter? _tokenLimiter;
    private readonly DailyTokenBudget? _dailyBudget;

    public ProviderRateLimitCoordinator(ProviderConfig config, DailyTokenBudget? dailyBudget = null)
    {
        _dailyBudget = dailyBudget;
        ProviderName = config.Provider;
        RequestsPerMinute = Math.Max(0, config.Rpm);
        TokensPerMinute = Math.Max(0, config.Tpm);

        if (RequestsPerMinute > 0)
            _requestLimiter = CreateSlidingWindowLimiter(RequestsPerMinute, 20);
        if (TokensPerMinute > 0)
            // QueueLimit is cumulative token permits, not a request count.
            _tokenLimiter = CreateSlidingWindowLimiter(TokensPerMinute, TokensPerMinute);
    }

    public string ProviderName { get; }
    public int RequestsPerMinute { get; }
    public int TokensPerMinute { get; }
    public bool IsEnabled => _requestLimiter != null || _tokenLimiter != null;
    public string Description => $"RPM {(RequestsPerMinute > 0 ? RequestsPerMinute : "unlimited")} / " +
                                 $"TPM {(TokensPerMinute > 0 ? TokensPerMinute : "unlimited")}";

    public async ValueTask WaitAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        int estimatedTokens = EstimateTokenBudget(messages, options);
        if (_tokenLimiter != null && estimatedTokens > TokensPerMinute)
            throw new ProviderTpmLimitException(ProviderName);
        _dailyBudget?.Reserve(estimatedTokens);
        if (_requestLimiter != null)
            await AcquireAsync(_requestLimiter, 1, "RPM", cancellationToken).ConfigureAwait(false);

        if (_tokenLimiter != null)
        {
            await AcquireAsync(_tokenLimiter, estimatedTokens, "TPM", cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask AcquireAsync(
        RateLimiter limiter,
        int permitCount,
        string limitName,
        CancellationToken cancellationToken)
    {
        using RateLimitLease lease = await limiter
            .AcquireAsync(Math.Max(1, permitCount), cancellationToken)
            .ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new ProviderQueueLimitException(ProviderName, limitName);
    }

    private static SlidingWindowRateLimiter CreateSlidingWindowLimiter(int permitLimit, int queueLimit)
        => new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 60,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit,
            AutoReplenishment = true
        });

    internal static int EstimateTokenBudget(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options)
    {
        double inputTokens = 0d;
        foreach (ChatMessage message in messages)
        {
            string text = message.Text ?? string.Empty;
            foreach (char character in text)
            {
                if (char.IsWhiteSpace(character)) continue;
                inputTokens += character <= 0x7F ? 0.25d : 1d;
            }
        }

        int outputBudget = Math.Max(
            0,
            options?.MaxOutputTokens ?? DefaultEstimatedOutputTokens);
        return Math.Max(1, (int)Math.Ceiling(inputTokens) + outputBudget);
    }

    public void Dispose()
    {
        _requestLimiter?.Dispose();
        _tokenLimiter?.Dispose();
    }
}

internal sealed class DailyTokenBudget(DeploymentOptions deployment)
{
    private readonly object _sync = new();
    private DateOnly _day = DateOnly.FromDateTime(DateTime.UtcNow);
    private long _estimatedTokens;

    public void Reserve(int estimatedTokens)
    {
        lock (_sync)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != _day)
            {
                _day = today;
                _estimatedTokens = 0;
            }
            if (_estimatedTokens + estimatedTokens > deployment.MaxEstimatedTokensPerDay)
                throw new SessionAiLimitException("server_daily_ai_limit");
            _estimatedTokens += estimatedTokens;
        }
    }
}

internal sealed class ProviderRateLimitStore(DeploymentOptions deployment) : IDisposable
{
    private readonly ConcurrentDictionary<string, ProviderRateLimitCoordinator> _limiters =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly DailyTokenBudget _dailyBudget = new(deployment);

    public ProviderRateLimitCoordinator Get(ProviderConfig config)
        => _limiters.GetOrAdd(config.Provider, _ => new ProviderRateLimitCoordinator(config, _dailyBudget));

    public void Dispose()
    {
        foreach (ProviderRateLimitCoordinator limiter in _limiters.Values) limiter.Dispose();
    }
}

internal sealed class SessionCallBudget(DeploymentOptions deployment)
{
    private readonly object _sync = new();
    private int _calls;
    private long _estimatedTokens;
    private long _measuredTokens;
    private int _measuredCalls;
    private int _unmeasuredCalls;
    private int _failedCalls;

    public int Reserve(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        int estimatedTokens = ProviderRateLimitCoordinator.EstimateTokenBudget(messages, options);
        lock (_sync)
        {
            if (_calls >= deployment.MaxCallsPerSession)
                throw new SessionAiLimitException("session_call_limit", SnapshotLocked());
            if (_estimatedTokens + estimatedTokens > deployment.MaxEstimatedTokensPerSession)
                throw new SessionAiLimitException("session_token_limit", SnapshotLocked());
            _calls++;
            _estimatedTokens += estimatedTokens;
        }
        return estimatedTokens;
    }

    public void Release(int reservedTokens)
    {
        lock (_sync)
        {
            _calls--;
            _estimatedTokens -= reservedTokens;
        }
    }

    public void Reconcile(int reservedTokens, ChatResponse response)
        => Reconcile(reservedTokens, response.Usage);

    public void Reconcile(int reservedTokens, UsageDetails? usage)
    {
        long? actualTokens = usage?.TotalTokenCount;
        if (actualTokens is not > 0 && (usage?.InputTokenCount is > 0 || usage?.OutputTokenCount is > 0))
            actualTokens = (usage.InputTokenCount ?? 0) + (usage.OutputTokenCount ?? 0);
        lock (_sync)
        {
            if (actualTokens is > 0)
            {
                _estimatedTokens = Math.Max(0, _estimatedTokens - reservedTokens + actualTokens.Value);
                _measuredTokens += actualTokens.Value;
                _measuredCalls++;
            }
            else
            {
                // Keep the conservative reservation when a Provider gives no usage data.
                _unmeasuredCalls++;
            }
        }
    }

    public void RecordFailure()
    {
        lock (_sync) _failedCalls++;
    }

    private SessionUsageSnapshot SnapshotLocked()
        => new(_calls, _estimatedTokens, _measuredTokens, _measuredCalls,
            _unmeasuredCalls, _failedCalls,
            deployment.MaxCallsPerSession, deployment.MaxEstimatedTokensPerSession);
}

internal sealed class ProviderRateLimitingChatClient(
    IChatClient innerClient,
    ProviderRateLimitCoordinator coordinator,
    SessionCallBudget budget)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> requestMessages = Materialize(messages);
        int reservedTokens = budget.Reserve(requestMessages, options);
        try
        {
            await coordinator.WaitAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            budget.Release(reservedTokens);
            throw;
        }
        ChatResponse response;
        try
        {
            response = await base.GetResponseAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The Provider might have billed a failed request, so retain its reservation.
            budget.RecordFailure();
            throw;
        }
        budget.Reconcile(reservedTokens, response);
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> requestMessages = Materialize(messages);
        int reservedTokens = budget.Reserve(requestMessages, options);
        try
        {
            await coordinator.WaitAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            budget.Release(reservedTokens);
            throw;
        }
        UsageDetails? usage = null;
        bool completed = false;
        try
        {
            await foreach (ChatResponseUpdate update in base
                               .GetStreamingResponseAsync(requestMessages, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                foreach (AIContent content in update.Contents)
                    if (content is UsageContent reportedUsage) usage = reportedUsage.Details;
                yield return update;
            }
            completed = true;
        }
        finally
        {
            if (completed) budget.Reconcile(reservedTokens, usage);
            else budget.RecordFailure();
        }
    }

    private static IReadOnlyList<ChatMessage> Materialize(IEnumerable<ChatMessage> messages)
        => messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
}

internal static class ProviderRateLimitingChatClientExtensions
{
    public static IChatClient UseProviderRateLimits(
        this IChatClient chatClient,
        ProviderRateLimitCoordinator coordinator,
        SessionCallBudget budget)
    {
        return chatClient
            .AsBuilder()
            .Use(innerClient => new ProviderRateLimitingChatClient(innerClient, coordinator, budget))
            .Build();
    }
}
