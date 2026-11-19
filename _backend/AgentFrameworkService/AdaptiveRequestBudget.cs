namespace MafiaSimulation.AgentFrameworkService;

internal static class AdaptiveRequestBudget
{
    public const int ConcurrentWorkLimit = 2;
    public const int ModelCallTimeoutSeconds = 120;
    public const int MaximumEstimatedCalls = 128;
    public const int MaximumMeetingStatements = 5;
    public const int MinimumSeconds = 180;
    public const int MaximumSeconds = 1800;
    public static int Calculate(int npcCount, int outstanding, int calls, int rpm, int tpm, int tokensPerCall = 4096)
    {
        // Two execution slots per session; include contention from the same Provider.
        double work = Math.Clamp(calls, 1, MaximumEstimatedCalls) + Math.Max(0, outstanding) + Math.Max(0, npcCount - 1);
        double seconds = 30 + Math.Ceiling(work / ConcurrentWorkLimit) * ModelCallTimeoutSeconds;
        if (rpm > 0) seconds += Math.Ceiling(work / rpm) * 60;
        if (tpm > 0) seconds += Math.Ceiling(work * Math.Clamp(tokensPerCall, 128, 32768) / tpm) * 60;
        return (int)Math.Clamp(seconds, MinimumSeconds, MaximumSeconds);
    }
}

public sealed record RequestTimingResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("request_timeout_seconds")] int RequestTimeoutSeconds,
    [property: System.Text.Json.Serialization.JsonPropertyName("queue_depth")] int QueueDepth,
    [property: System.Text.Json.Serialization.JsonPropertyName("provider_npc_count")] int ProviderNpcCount);
