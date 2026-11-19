namespace MafiaSimulation.AgentFrameworkService;

/// <summary>Deterministic checks applied after the moderator proposes an event outcome.</summary>
internal static class ModeratorEventRules
{
    internal static string SelectSecretRecipientFromVotes(int currentDay, string mafiaId,
        IEnumerable<string> aliveIds, IEnumerable<GameEventRequest> history, Func<int, int> chooseIndex)
    {
        if (currentDay <= 1 || string.IsNullOrWhiteSpace(mafiaId)) return "";
        string[] eligible = aliveIds.Where(id => !string.IsNullOrWhiteSpace(id) && id != mafiaId)
            .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (eligible.Length == 0) return "";
        var validVotes = history.Where(e => e.Day > 0 && e.Day < currentDay &&
            string.Equals(e.Phase, "Voting", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Tag, "Vote", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Visibility, "discussion", StringComparison.OrdinalIgnoreCase) &&
            e.TargetAgentId == mafiaId && eligible.Contains(e.OwnerAgentId, StringComparer.Ordinal))
            .GroupBy(e => (e.OwnerAgentId, e.Day)).Select(group => group.First()).ToArray();
        var ranks = eligible.Select(id => new { Id = id,
            Count = validVotes.Count(e => e.OwnerAgentId == id),
            Recent = validVotes.Where(e => e.OwnerAgentId == id).Select(e => e.Day).DefaultIfEmpty(0).Max() }).ToArray();
        int maximum = ranks.Max(item => item.Count);
        int latest = ranks.Where(item => item.Count == maximum).Max(item => item.Recent);
        string[] tied = ranks.Where(item => item.Count == maximum && item.Recent == latest).Select(item => item.Id).ToArray();
        int index = chooseIndex(tied.Length);
        if (index < 0 || index >= tied.Length) throw new ArgumentOutOfRangeException(nameof(chooseIndex));
        return tied[index];
    }

    internal static string SelectSecretRecipient(string criterion, int currentDay, IEnumerable<string> aliveIds,
        IEnumerable<GameEventRequest> previousDiscussion)
    {
        if (currentDay <= 1) return "";
        string[] eligible = aliveIds.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (eligible.Length == 0) return "";
        var counts = eligible.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        foreach (GameEventRequest entry in previousDiscussion)
        {
            if (entry.Day != currentDay - 1 ||
                !string.Equals(entry.Visibility, "discussion", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entry.Phase, "Meeting", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(entry.Content) || !counts.ContainsKey(entry.OwnerAgentId)) continue;
            counts[entry.OwnerAgentId]++;
        }
        if (counts.Values.All(count => count == 0)) return "";
        return criterion switch
        {
            "most_messages" => eligible.OrderByDescending(id => counts[id]).ThenBy(id => id, StringComparer.Ordinal).First(),
            "least_messages" => eligible.OrderBy(id => counts[id]).ThenBy(id => id, StringComparer.Ordinal).First(),
            _ => ""
        };
    }

    internal static bool IsSupportedDiscovery(string discovererId, string evidence,
        IReadOnlyList<string> participants, IReadOnlyList<AgentAction> dialogue,
        IEnumerable<string> previouslyRecordedLines)
    {
        if (participants.Count != 2 || !participants.Contains(discovererId, StringComparer.Ordinal) ||
            string.IsNullOrWhiteSpace(evidence)) return false;
        string excerpt = evidence.Trim();
        if (excerpt.Length < 4) return false;
        // A claim must be heard from the other participant; the model cannot invent a source.
        if (!dialogue.Any(line => line.ActorId != discovererId &&
                participants.Contains(line.ActorId, StringComparer.Ordinal) &&
                (line.DialogueContent ?? "").Contains(excerpt, StringComparison.Ordinal))) return false;
        return !previouslyRecordedLines.Any(line => (line ?? "").Contains(excerpt, StringComparison.Ordinal));
    }
}
