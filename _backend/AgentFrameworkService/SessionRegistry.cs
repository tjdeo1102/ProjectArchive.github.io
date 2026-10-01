using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MafiaSimulation.AgentFrameworkService;

internal sealed record RegisteredSession(GameSession Session, string Token);

internal sealed class SessionRegistry(DeploymentOptions deployment) : IAsyncDisposable
{
    private sealed class Entry(GameSession session, byte[] tokenHash)
    {
        private long _lastAccessTicks = DateTime.UtcNow.Ticks;

        public GameSession Session { get; } = session;
        public byte[] TokenHash { get; } = tokenHash;
        public DateTime LastAccessUtc => new(Interlocked.Read(ref _lastAccessTicks), DateTimeKind.Utc);
        public void Touch() => Interlocked.Exchange(ref _lastAccessTicks, DateTime.UtcNow.Ticks);
    }

    private readonly ConcurrentDictionary<string, Entry> _sessions = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _slots = new(deployment.MaxSessions, deployment.MaxSessions);
    private readonly ProviderRateLimitStore _globalLimiters = new(deployment);

    public async Task<RegisteredSession> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!await _slots.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("The public test is at capacity. Please try again later.");

        try
        {
            string sessionId = Guid.NewGuid().ToString("N");
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            GameSession session = await GameSession.CreateAsync(
                request with { SessionId = sessionId }, deployment, _globalLimiters, cancellationToken);
            if (!_sessions.TryAdd(sessionId, new Entry(session, HashToken(token))))
            {
                await session.DisposeAsync();
                throw new InvalidOperationException("Could not register a new game session.");
            }
            return new RegisteredSession(session, token);
        }
        catch
        {
            _slots.Release();
            throw;
        }
    }

    public GameSession Get(string sessionId, string? token)
    {
        if (!_sessions.TryGetValue(sessionId, out Entry? entry))
            throw new KeyNotFoundException("Unknown session.");
        if (string.IsNullOrWhiteSpace(token) ||
            !CryptographicOperations.FixedTimeEquals(entry.TokenHash, HashToken(token)))
            throw new UnauthorizedAccessException("Invalid session token.");
        entry.Touch();
        return entry.Session;
    }

    public async Task RemoveAsync(string sessionId, string? token)
    {
        Get(sessionId, token);
        await RemoveRegisteredAsync(sessionId);
    }

    public async Task CleanupExpiredAsync()
    {
        DateTime now = DateTime.UtcNow;
        foreach ((string sessionId, Entry entry) in _sessions)
        {
            if (now - entry.LastAccessUtc >= deployment.SessionIdleTimeout)
                await RemoveRegisteredAsync(sessionId);
        }
    }

    private async Task RemoveRegisteredAsync(string sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out Entry? removed)) return;
        try { await removed.Session.DisposeAsync(); }
        finally { _slots.Release(); }
    }

    private static byte[] HashToken(string token)
        => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public async ValueTask DisposeAsync()
    {
        foreach (string sessionId in _sessions.Keys) await RemoveRegisteredAsync(sessionId);
        _globalLimiters.Dispose();
        _slots.Dispose();
    }
}

internal sealed class SessionCleanupService(SessionRegistry registry) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await registry.CleanupExpiredAsync();
    }
}
