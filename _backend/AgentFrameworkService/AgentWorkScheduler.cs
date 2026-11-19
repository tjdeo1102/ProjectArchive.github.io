using System.Collections.Concurrent;
using System.Diagnostics;

namespace MafiaSimulation.AgentFrameworkService;

// Sessions may run different NPCs concurrently, but an NPC's AgentSession is exclusive.
internal sealed class AgentWorkScheduler : IAsyncDisposable
{
    private readonly SemaphoreSlim _capacity = new(AdaptiveRequestBudget.ConcurrentWorkLimit, AdaptiveRequestBudget.ConcurrentWorkLimit);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _agents = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _sync = new();
    private TaskCompletionSource _drained = Completed();
    private int _active;
    private readonly ConcurrentDictionary<string, int> _outstanding = new();
    private readonly ConcurrentDictionary<string, int> _running = new();
    public int QueuedFor(IEnumerable<string> ids) => ids.Sum(id => Math.Max(0, _outstanding.GetValueOrDefault(id) - _running.GetValueOrDefault(id)));
    private bool _stopping;

    public async Task<T> RunAsync<T>(string agentId, Func<CancellationToken, Task<T>> work, CancellationToken token)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_active++ == 0) _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        _outstanding.AddOrUpdate(agentId, 1, (_, n) => n + 1);
        var timer = Stopwatch.StartNew();
        long queueMs = 0;
        bool agentEntered = false, slotEntered = false;
        string outcome = "cancelled";
        var agent = _agents.GetOrAdd(agentId, _ => new SemaphoreSlim(1, 1));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdown.Token);
        try
        {
            await agent.WaitAsync(linked.Token);
            agentEntered = true;
            await _capacity.WaitAsync(linked.Token);
            slotEntered = true;
            _running[agentId] = 1;
            queueMs = timer.ElapsedMilliseconds;
            var result = await work(linked.Token);
            outcome = "success";
            return result;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            outcome = e is TimeoutException ? "timeout" : "failed";
            throw;
        }
        finally
        {
            if (!slotEntered) queueMs = timer.ElapsedMilliseconds;
            if (slotEntered) { _running[agentId] = 0; _capacity.Release(); }
            if (agentEntered) agent.Release();
            // Never include IDs, prompts, keys, response text or exception messages.
            Console.WriteLine($"[AI timing] queue_ms={queueMs} work_ms={timer.ElapsedMilliseconds - queueMs} outcome={outcome}");
            _outstanding.AddOrUpdate(agentId, 0, (_, n) => Math.Max(0, n - 1));
            lock (_sync) if (--_active == 0) _drained.TrySetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task drained;
        lock (_sync) { _stopping = true; drained = _drained.Task; }
        _shutdown.Cancel();
        await drained;
        foreach (var gate in _agents.Values) gate.Dispose();
        _capacity.Dispose();
        _shutdown.Dispose();
    }

    private static TaskCompletionSource Completed()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
