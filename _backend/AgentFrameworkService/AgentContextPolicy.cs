using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafiaSimulation.AgentFrameworkService;

internal static class AgentContextPolicy
{
    internal const int RecentResponseMessages = 1;

    // Canonical game events live in GameSession. Do not store another copy of each
    // rendered memory block in the Framework's conversation history on every run.
    #pragma warning disable MEAI001 // Pinned Framework reducer API, covered by context regression tests.
    public static InMemoryChatHistoryProvider CreateHistory() => new(new()
    {
        StorageInputRequestMessageFilter = _ => Array.Empty<ChatMessage>(),
        ChatReducer = new MessageCountingChatReducer(RecentResponseMessages),
        ReducerTriggerEvent = InMemoryChatHistoryProviderOptions.ChatReducerTriggerEvent.AfterMessageAdded
    });
    #pragma warning restore MEAI001
}
