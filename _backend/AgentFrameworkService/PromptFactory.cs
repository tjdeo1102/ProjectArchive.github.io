using System.Globalization;
using System.Text.RegularExpressions;

namespace MafiaSimulation.AgentFrameworkService;

internal static partial class PromptFactory
{
    public static string BuildAgentInstructions(AgentSpec spec, PromptSet prompts, string language)
    {
        string role = spec.IsMafia ? prompts.MafiaRoleLabel : prompts.CitizenRoleLabel;
        string roleGuidance = spec.IsMafia ? prompts.MafiaRoleGuidance : prompts.CitizenRoleGuidance;
        var values = new Dictionary<string, string>
        {
            ["agentId"] = spec.AgentId,
            ["agentName"] = spec.AgentName,
            ["personaProfile"] = ValueOrDefault(spec.Persona, prompts.DefaultPersona),
            ["roleDescription"] = role,
            ["roleGuidance"] = roleGuidance,
            ["personaGuidance"] = string.Empty,
            ["strategySummary"] = prompts.StrategyDefault,
            ["currentDay"] = string.Empty,
            ["currentPhase"] = string.Empty,
            ["aliveCount"] = string.Empty,
            ["agentCount"] = string.Empty,
            ["aliveAgentList"] = string.Empty
        };
        values["environment"] = Render(prompts.Environment, values);
        return Render(prompts.AgentInstructions, values);
    }

    public static string BuildActionTask(
        AgentActionRequest request,
        PromptSet prompts,
        IReadOnlyList<GameEventRequest> memories)
    {
        string template = request.ActionType switch
        {
            "talk_decision" => prompts.DialogueDecision,
            "dialogue" => prompts.Dialogue,
            "patrol" => prompts.Patrol,
            _ => throw new ArgumentException($"Unsupported action type: {request.ActionType}")
        };
        var values = CommonValues(request.TargetId, request.TargetName, request.PlayerMessage);
        values["actionType"] = request.ActionType;
        values["turn"] = request.Turn.ToString(CultureInfo.InvariantCulture);
        values["currentDay"] = request.CurrentDay.ToString(CultureInfo.InvariantCulture);
        values["currentPhase"] = request.CurrentPhase;
        values["positionX"] = request.PositionX.ToString("F2", CultureInfo.InvariantCulture);
        values["positionZ"] = request.PositionZ.ToString("F2", CultureInfo.InvariantCulture);
        values["patrolRangeX"] = request.PatrolRangeX.ToString("F2", CultureInfo.InvariantCulture);
        values["patrolRangeZ"] = request.PatrolRangeZ.ToString("F2", CultureInfo.InvariantCulture);
        values["memoryBlock"] = BuildMemoryBlock(prompts, memories);
        values["memoryContext"] = values["memoryBlock"];
        return Join(Render(template, values), Render(prompts.ActionContext, values));
    }

    public static string BuildMeetingTask(
        PromptSet prompts,
        IReadOnlyList<string> aliveIds,
        int day,
        int rounds)
    {
        string aliveAgentList = string.Join(", ", aliveIds);
        var values = new Dictionary<string, string>
        {
            ["candidateList"] = aliveAgentList,
            ["aliveAgentList"] = aliveAgentList,
            ["currentDay"] = day.ToString(CultureInfo.InvariantCulture),
            ["rounds"] = rounds.ToString(CultureInfo.InvariantCulture),
            ["moderator"] = prompts.Moderator
        };
        values["meetingPrompt"] = Render(prompts.Meeting, values);
        return Render(prompts.MeetingContext, values);
    }

    public static string BuildAgentTurnTask(
        PromptSet prompts,
        string task,
        AgentSpec speaker,
        IReadOnlyList<GameEventRequest> memories,
        IReadOnlyList<string> currentSceneHistory)
    {
        string sceneHistory = currentSceneHistory.Count == 0
            ? prompts.SceneHistoryEmpty
            : string.Join("\n", currentSceneHistory.TakeLast(12).Select(line =>
                Render(prompts.HistoryEntry, new Dictionary<string, string> { ["content"] = line })));
        return Render(prompts.AgentTurnContext, new Dictionary<string, string>
        {
            ["task"] = task,
            ["agentId"] = speaker.AgentId,
            ["agentName"] = speaker.AgentName,
            ["personaProfile"] = ValueOrDefault(speaker.Persona, prompts.DefaultPersona),
            ["memoryBlock"] = BuildMemoryBlock(prompts, memories),
            ["sceneHistory"] = sceneHistory
        });
    }

    public static string BuildDialogueTask(
        PromptSet prompts,
        IReadOnlyList<AgentSpec> participants,
        int day,
        int minTurns,
        int maxTurns,
        IReadOnlyList<string> previousDialogue)
    {
        string participantProfiles = string.Join("\n", participants.Select(participant =>
            Render(prompts.ParticipantProfile, new Dictionary<string, string>
            {
                ["agentId"] = participant.AgentId,
                ["agentName"] = participant.AgentName,
                ["personaProfile"] = ValueOrDefault(participant.Persona, prompts.DefaultPersona)
            })));
        string previousDialogueText = string.Join("\n", previousDialogue.Select(line =>
            Render(prompts.HistoryEntry, new Dictionary<string, string> { ["content"] = line })));
        string relationshipContext = previousDialogue.Count == 0
            ? prompts.FirstEncounter
            : Render(prompts.ReturningEncounter, new Dictionary<string, string>
            {
                ["previousDialogue"] = previousDialogueText
            });
        return Render(prompts.DialogueScene, new Dictionary<string, string>
        {
            ["currentDay"] = day.ToString(CultureInfo.InvariantCulture),
            ["participantIds"] = string.Join(", ", participants.Select(participant => participant.AgentId)),
            ["participantProfiles"] = participantProfiles,
            ["firstSpeakerId"] = participants[0].AgentId,
            ["relationshipContext"] = relationshipContext,
            ["minTurns"] = minTurns.ToString(CultureInfo.InvariantCulture),
            ["maxTurns"] = maxTurns.ToString(CultureInfo.InvariantCulture)
        });
    }

    public static string BuildPhaseTask(
        PromptSet prompts,
        string prompt,
        string phase,
        string actorId,
        IReadOnlyList<string> aliveIds,
        int day,
        IReadOnlyList<GameEventRequest> memories)
    {
        string aliveAgentList = string.Join(", ", aliveIds);
        var values = new Dictionary<string, string>
        {
            ["candidateList"] = aliveAgentList,
            ["aliveAgentList"] = aliveAgentList,
            ["selfId"] = actorId,
            ["currentDay"] = day.ToString(CultureInfo.InvariantCulture),
            ["memoryBlock"] = BuildMemoryBlock(prompts, memories)
        };
        values["memoryContext"] = values["memoryBlock"];
        values["phasePrompt"] = Render(prompt, values);
        return Render(prompts.PhaseContext, values);
    }

    private static Dictionary<string, string> CommonValues(string targetId, string targetName, string message) => new()
    {
        ["targetId"] = targetId,
        ["targetName"] = targetName,
        ["playerMessage"] = message,
        ["targetAgent.agentId"] = targetId,
        ["targetAgent.agentName"] = targetName,
        ["requester.agentId"] = targetId,
        ["requester.agentName"] = targetName,
        ["listenerId"] = targetId,
        ["listenerName"] = targetName,
        ["currentLastMessage"] = message,
        ["sessionHistoryBlock"] = string.Empty,
        ["recentSelfUtteranceGuard"] = string.Empty,
        ["lastUtteranceGuard"] = string.Empty,
        ["intentGuidanceBlock"] = string.Empty,
        ["recentIntentGuard"] = string.Empty,
        ["topicFatigueBlock"] = string.Empty,
        ["diverseTopicAnglesBlock"] = string.Empty
    };

    private static string BuildMemoryBlock(PromptSet prompts, IReadOnlyList<GameEventRequest> memories)
    {
        if (memories.Count == 0) return prompts.MemoryEmpty;

        int recentStart = Math.Max(0, memories.Count - 24);
        IEnumerable<GameEventRequest> selected = memories.Where((memory, index) =>
            index >= recentStart ||
            memory.Visibility.Equals("private", StringComparison.OrdinalIgnoreCase) ||
            memory.Tag.Equals("Vote", StringComparison.OrdinalIgnoreCase) ||
            memory.Tag.Equals("Night", StringComparison.OrdinalIgnoreCase) ||
            memory.Tag.Equals("System", StringComparison.OrdinalIgnoreCase));
        string entries = string.Join("\n", selected.Select(memory =>
            Render(prompts.MemoryEntry, new Dictionary<string, string>
            {
                ["day"] = memory.Day.ToString(CultureInfo.InvariantCulture),
                ["phase"] = memory.Phase,
                ["tag"] = memory.Tag,
                ["content"] = memory.Content
            })));
        return Join(prompts.MemoryHeader, entries);
    }

    private static string Render(string? template, IReadOnlyDictionary<string, string> values)
    {
        string result = (template ?? string.Empty).Trim();
        foreach ((string key, string value) in values)
            result = result.Replace("{" + key + "}", value ?? string.Empty, StringComparison.Ordinal);
        return UnknownRuntimeTokenRegex().Replace(UnknownTokenRegex().Replace(result, string.Empty), string.Empty).Trim();
    }

    private static string Join(params string?[] values) =>
        string.Join("\n\n", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

    private static string ValueOrDefault(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;

    [GeneratedRegex(@"\{[A-Za-z][A-Za-z0-9_.]+\}")]
    private static partial Regex UnknownTokenRegex();

    [GeneratedRegex(@"\{runtime\.[A-Za-z0-9_.]+\}")]
    private static partial Regex UnknownRuntimeTokenRegex();
}
