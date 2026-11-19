namespace MafiaSimulation.AgentFrameworkService;

internal sealed class AssessmentBoard
{
    internal const string AbstainTarget = "__abstain__";
    private readonly Dictionary<string, double> _scores = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, double> Snapshot() => new Dictionary<string, double>(_scores, StringComparer.Ordinal);
    public void Clear() => _scores.Clear();

    public void Update(IEnumerable<AssessmentScore> scores, IEnumerable<string> candidates, string ownerId)
    {
        var allowed = candidates.Where(id => !string.IsNullOrWhiteSpace(id) && id != ownerId).ToHashSet(StringComparer.Ordinal);
        allowed.Add(AbstainTarget);
        foreach (AssessmentScore score in scores ?? Enumerable.Empty<AssessmentScore>())
            if (score != null && allowed.Contains(score.TargetId) && double.IsFinite(score.Probability))
                _scores[score.TargetId] = Math.Clamp(score.Probability, 0, 1);
    }

    public string Select(IEnumerable<string> eligibleCandidates, string ownerId)
    {
        string target = eligibleCandidates.Distinct(StringComparer.Ordinal)
            .Where(id => id != ownerId && _scores.TryGetValue(id, out double score) && score > 0)
            .OrderByDescending(id => _scores[id]).ThenBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;
        if (target.Length == 0) return string.Empty;
        if (_scores.TryGetValue(AbstainTarget, out double abstain) && abstain >= _scores[target]) return string.Empty;
        return target;
    }
}
