namespace MafiaSimulation.AgentFrameworkService;

internal static class TownNewsValidation
{
    public static TownNewsBatch Filter(TownNewsBatch? batch, IEnumerable<AgentSpec> agents)
    {
        batch ??= new TownNewsBatch();
        string[] forbidden = agents.SelectMany(agent => new[] { agent.AgentId, agent.AgentName })
            .Where(value => !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 2)
            .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string Clean(string? text, int maxLength)
        {
            string value = (text ?? "").Trim();
            if (value.Length > maxLength || forbidden.Any(name => value.Contains(name, StringComparison.OrdinalIgnoreCase)))
                return "";
            return value;
        }
        var result = new TownNewsBatch
        {
            ArrivalStory = Clean(batch.ArrivalStory, 500),
            FavoriteFoodHint = Clean(batch.FavoriteFoodHint, 160),
            HobbyHint = Clean(batch.HobbyHint, 160)
        };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TownNewsItem? item in batch.Items ?? new List<TownNewsItem>())
        {
            if (item == null) continue;
            string headline = Clean(item.Headline, 100);
            string body = Clean(item.Body, 500);
            string genre = Clean(item.Genre, 40);
            if (headline.Length == 0 || body.Length == 0 || !seen.Add(headline)) continue;
            result.Items.Add(new TownNewsItem { Genre = genre, Headline = headline, Body = body });
            if (result.Items.Count == 64) break;
        }
        return result;
    }
}
