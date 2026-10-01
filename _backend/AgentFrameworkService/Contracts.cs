using System.Text.Json;
using System.Text.Json.Serialization;

namespace MafiaSimulation.AgentFrameworkService;

public sealed record ProviderConfig(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("apiKey")] string ApiKey,
    [property: JsonPropertyName("modelName")] string ModelName,
    [property: JsonPropertyName("customBaseUrl")] string CustomBaseUrl,
    [property: JsonPropertyName("options")] Dictionary<string, JsonElement>? Options,
    [property: JsonPropertyName("rpm")] int Rpm,
    [property: JsonPropertyName("tpm")] int Tpm,
    [property: JsonPropertyName("allowedModels")] List<string>? AllowedModels = null);

public sealed record ProviderCollection(
    [property: JsonPropertyName("providerOverride")] string ProviderOverride,
    [property: JsonPropertyName("providers")] List<ProviderConfig> Providers);

public sealed record AgentSpec(
    [property: JsonPropertyName("agent_id")] string AgentId,
    [property: JsonPropertyName("agent_name")] string AgentName,
    [property: JsonPropertyName("is_mafia")] bool IsMafia,
    [property: JsonPropertyName("persona")] string Persona,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model_override")] string ModelOverride);

public sealed record PromptSet(
    [property: JsonPropertyName("environment")] string Environment,
    [property: JsonPropertyName("moderator")] string Moderator,
    [property: JsonPropertyName("dialogue")] string Dialogue,
    [property: JsonPropertyName("dialogue_decision")] string DialogueDecision,
    [property: JsonPropertyName("meeting")] string Meeting,
    [property: JsonPropertyName("voting")] string Voting,
    [property: JsonPropertyName("night")] string Night,
    [property: JsonPropertyName("patrol")] string Patrol,
    [property: JsonPropertyName("agent_instructions")] string AgentInstructions,
    [property: JsonPropertyName("action_context")] string ActionContext,
    [property: JsonPropertyName("meeting_context")] string MeetingContext,
    [property: JsonPropertyName("agent_turn_context")] string AgentTurnContext,
    [property: JsonPropertyName("dialogue_scene")] string DialogueScene,
    [property: JsonPropertyName("first_encounter")] string FirstEncounter,
    [property: JsonPropertyName("returning_encounter")] string ReturningEncounter,
    [property: JsonPropertyName("phase_context")] string PhaseContext,
    [property: JsonPropertyName("memory_empty")] string MemoryEmpty,
    [property: JsonPropertyName("memory_header")] string MemoryHeader,
    [property: JsonPropertyName("memory_entry")] string MemoryEntry,
    [property: JsonPropertyName("scene_history_empty")] string SceneHistoryEmpty,
    [property: JsonPropertyName("default_persona")] string DefaultPersona,
    [property: JsonPropertyName("mafia_role_label")] string MafiaRoleLabel,
    [property: JsonPropertyName("citizen_role_label")] string CitizenRoleLabel,
    [property: JsonPropertyName("mafia_role_guidance")] string MafiaRoleGuidance,
    [property: JsonPropertyName("citizen_role_guidance")] string CitizenRoleGuidance,
    [property: JsonPropertyName("strategy_default")] string StrategyDefault,
    [property: JsonPropertyName("participant_profile")] string ParticipantProfile,
    [property: JsonPropertyName("history_entry")] string HistoryEntry);

public sealed record CreateSessionRequest(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("provider_config_json")] string ProviderConfigJson,
    [property: JsonPropertyName("prompts")] PromptSet Prompts,
    [property: JsonPropertyName("agents")] List<AgentSpec> Agents);

public sealed record SessionResponse(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("session_token")] string SessionToken,
    [property: JsonPropertyName("agent_ids")] List<string> AgentIds,
    [property: JsonPropertyName("provider_statuses")] List<ProviderStatus> ProviderStatuses);

public sealed record ProviderStatus(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("available")] bool Available,
    [property: JsonPropertyName("message")] string Message);

public sealed class AgentAction
{
    [JsonPropertyName("actor_id")] public string ActorId { get; set; } = "";
    [JsonPropertyName("action")] public string Action { get; set; } = "NONE";
    [JsonPropertyName("target_id")] public string TargetId { get; set; } = "";
    [JsonPropertyName("dialogue_content")] public string DialogueContent { get; set; } = "";
    [JsonPropertyName("dialogue_intent")] public string DialogueIntent { get; set; } = "";
    [JsonPropertyName("reasoning")] public string Reasoning { get; set; } = "";
    [JsonPropertyName("continue_dialogue")] public bool ContinueDialogue { get; set; }
    [JsonPropertyName("target_x")] public float TargetX { get; set; }
    [JsonPropertyName("target_z")] public float TargetZ { get; set; }
}

public sealed record AgentActionRequest(
    [property: JsonPropertyName("operation_id")] string OperationId,
    [property: JsonPropertyName("agent_id")] string AgentId,
    [property: JsonPropertyName("action_type")] string ActionType,
    [property: JsonPropertyName("target_id")] string TargetId,
    [property: JsonPropertyName("target_name")] string TargetName,
    [property: JsonPropertyName("recent_partner")] bool RecentPartner,
    [property: JsonPropertyName("player_message")] string PlayerMessage,
    [property: JsonPropertyName("turn")] int Turn,
    [property: JsonPropertyName("current_day")] int CurrentDay,
    [property: JsonPropertyName("current_phase")] string CurrentPhase,
    [property: JsonPropertyName("position_x")] float PositionX,
    [property: JsonPropertyName("position_z")] float PositionZ,
    [property: JsonPropertyName("patrol_range_x")] float PatrolRangeX,
    [property: JsonPropertyName("patrol_range_z")] float PatrolRangeZ);

public sealed record MeetingRequest(
    [property: JsonPropertyName("operation_id")] string OperationId,
    [property: JsonPropertyName("alive_agent_ids")] List<string> AliveAgentIds,
    [property: JsonPropertyName("speaker_agent_ids")] List<string>? SpeakerAgentIds,
    [property: JsonPropertyName("current_day")] int CurrentDay,
    [property: JsonPropertyName("rounds")] int Rounds);

public sealed record DialogueRequest(
    [property: JsonPropertyName("operation_id")] string OperationId,
    [property: JsonPropertyName("participant_ids")] List<string> ParticipantIds,
    [property: JsonPropertyName("current_day")] int CurrentDay,
    [property: JsonPropertyName("min_turns")] int MinTurns,
    [property: JsonPropertyName("max_turns")] int MaxTurns);

public sealed record PhaseDecisionRequest(
    [property: JsonPropertyName("operation_id")] string OperationId,
    [property: JsonPropertyName("alive_agent_ids")] List<string> AliveAgentIds,
    [property: JsonPropertyName("acting_agent_ids")] List<string>? ActingAgentIds,
    [property: JsonPropertyName("current_day")] int CurrentDay);

public sealed record ActionListResponse(
    [property: JsonPropertyName("actions")] List<AgentAction> Actions);

public sealed record OperationProgressResponse(
    [property: JsonPropertyName("actions")] List<AgentAction> Actions,
    [property: JsonPropertyName("next_offset")] int NextOffset,
    [property: JsonPropertyName("completed")] bool Completed);

public sealed record GameEventRequest(
    [property: JsonPropertyName("owner_agent_id")] string OwnerAgentId,
    [property: JsonPropertyName("target_agent_id")] string TargetAgentId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("visibility")] string Visibility,
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("tag")] string Tag);

public sealed record SessionStateResponse(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("state")] SessionState State);

public sealed record LoadSessionStateRequest(
    [property: JsonPropertyName("state")] SessionState State);

public sealed record SessionState(
    [property: JsonPropertyName("events")] Dictionary<string, List<GameEventRequest>> Events,
    [property: JsonPropertyName("transcripts")] Dictionary<string, List<string>> Transcripts);
