using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.Extensions.AI;

namespace MafiaSimulation.AgentFrameworkService;

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
            _requestLimiter = CreateSlidingWindowLimiter(RequestsPerMinute);
        if (TokensPerMinute > 0)
            _tokenLimiter = CreateSlidingWindowLimiter(TokensPerMinute);
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
            throw new InvalidOperationException($"Request estimate exceeds provider {ProviderName} TPM.");
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
            throw new InvalidOperationException(
                $"Unable to acquire {limitName} lease for provider {ProviderName}.");
    }

    private static SlidingWindowRateLimiter CreateSlidingWindowLimiter(int permitLimit)
        => new(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 60,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 20,
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
                throw new InvalidOperationException("The public AI budget has been reached for today.");
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

    public void Reserve(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        int estimatedTokens = ProviderRateLimitCoordinator.EstimateTokenBudget(messages, options);
        lock (_sync)
        {
            if (_calls >= deployment.MaxCallsPerSession ||
                _estimatedTokens + estimatedTokens > deployment.MaxEstimatedTokensPerSession)
                throw new InvalidOperationException("This game has reached its AI usage limit.");
            _calls++;
            _estimatedTokens += estimatedTokens;
        }
    }
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
        budget.Reserve(requestMessages, options);
        await coordinator.WaitAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        return await base.GetResponseAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> requestMessages = Materialize(messages);
        budget.Reserve(requestMessages, options);
        await coordinator.WaitAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        await foreach (ChatResponseUpdate update in base
                           .GetStreamingResponseAsync(requestMessages, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return update;
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
