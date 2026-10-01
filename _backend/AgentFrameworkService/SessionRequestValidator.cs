using System.Text.Json;

namespace MafiaSimulation.AgentFrameworkService;

internal static class SessionRequestValidator
{
    public static void Validate(CreateSessionRequest request, DeploymentOptions deployment)
    {
        if (request.Agents is not { Count: >= 3 and <= 12 })
            throw new ArgumentException("A game requires 3 to 12 agents.");
        if (request.Prompts is null || JsonSerializer.Serialize(request.Prompts).Length > 200_000)
            throw new ArgumentException("Prompt settings are missing or too large.");
        if (request.Language is not ("ko" or "en") ||
            (!deployment.LocalDevelopment && request.Language != "ko"))
            throw new ArgumentException("Unsupported game language.");
        if (!deployment.LocalDevelopment && string.IsNullOrWhiteSpace(request.ProviderConfigJson))
            throw new ArgumentException("A Provider JSON file is required for this game session.");
        if ((request.ProviderConfigJson?.Length ?? 0) > 100_000)
            throw new ArgumentException("Provider JSON is too large.");

        var agentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int mafiaCount = 0;
        foreach (AgentSpec agent in request.Agents)
        {
            if (agent is null ||
                !HasLength(agent.AgentId, 64) ||
                !HasLength(agent.AgentName, 80) ||
                !HasLength(agent.Provider, 80) ||
                (agent.ModelOverride?.Length ?? 0) > 128 ||
                (agent.Persona?.Length ?? 0) > 2_000 ||
                !agentIds.Add(agent.AgentId))
                throw new ArgumentException("Invalid or duplicate agent settings.");
            if (agent.IsMafia) mafiaCount++;
        }

        if (mafiaCount != 1)
            throw new ArgumentException("Exactly one mafia agent is required.");
    }

    public static void Validate(AgentActionRequest request)
    {
        ValidateOperationId(request.OperationId);
        if (!HasLength(request.AgentId, 64) ||
            request.ActionType is not ("talk_decision" or "dialogue" or "patrol") ||
            (request.PlayerMessage?.Length ?? 0) > 2_000 ||
            (request.TargetId?.Length ?? 0) > 64 ||
            (request.TargetName?.Length ?? 0) > 80 ||
            (request.CurrentPhase?.Length ?? 0) > 64 ||
            request.Turn is < 0 or > 100 || request.CurrentDay is < 0 or > 100)
            throw new ArgumentException("Invalid agent action request.");
    }

    public static void Validate(MeetingRequest request)
    {
        ValidateOperationId(request.OperationId);
        ValidateAgentIds(request.AliveAgentIds);
        if (request.SpeakerAgentIds != null) ValidateAgentIds(request.SpeakerAgentIds);
        if (request.SpeakerAgentIds != null &&
            request.SpeakerAgentIds.Any(id => !request.AliveAgentIds.Contains(id, StringComparer.Ordinal)))
            throw new ArgumentException("Meeting speakers must be alive agents.");
        if (request.Rounds is < 1 or > 4 || request.CurrentDay is < 0 or > 100)
            throw new ArgumentException("Invalid meeting request.");
    }

    public static void Validate(DialogueRequest request)
    {
        ValidateOperationId(request.OperationId);
        ValidateAgentIds(request.ParticipantIds);
        if (request.ParticipantIds.Count != 2 ||
            request.MinTurns is < 2 or > 6 ||
            request.MaxTurns < request.MinTurns || request.MaxTurns > 6 ||
            request.CurrentDay is < 0 or > 100)
            throw new ArgumentException("Invalid dialogue request.");
    }

    public static void Validate(PhaseDecisionRequest request)
    {
        ValidateOperationId(request.OperationId);
        ValidateAgentIds(request.AliveAgentIds);
        if (request.ActingAgentIds != null) ValidateAgentIds(request.ActingAgentIds);
        if (request.ActingAgentIds != null &&
            request.ActingAgentIds.Any(id => !request.AliveAgentIds.Contains(id, StringComparer.Ordinal)))
            throw new ArgumentException("Phase actors must be alive agents.");
        if (request.CurrentDay is < 0 or > 100)
            throw new ArgumentException("Invalid phase request.");
    }

    public static void Validate(GameEventRequest request)
    {
        if ((request.Content?.Length ?? 0) > 2_000 ||
            (request.OwnerAgentId?.Length ?? 0) > 64 ||
            (request.TargetAgentId?.Length ?? 0) > 64 ||
            (request.Phase?.Length ?? 0) > 64 ||
            (request.Tag?.Length ?? 0) > 64 ||
            request.Visibility is not ("public" or "private" or "discussion") ||
            request.Day is < 0 or > 100)
            throw new ArgumentException("Invalid game event.");
    }

    private static void ValidateOperationId(string? operationId)
    {
        if (!HasLength(operationId, 64))
            throw new ArgumentException("Invalid operation ID.");
    }

    private static void ValidateAgentIds(List<string>? ids)
    {
        if (ids is not { Count: >= 1 and <= 12 } ||
            ids.Any(id => !HasLength(id, 64)) ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
            throw new ArgumentException("Invalid agent ID list.");
    }

    private static bool HasLength(string? value, int maximum)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum;
}
