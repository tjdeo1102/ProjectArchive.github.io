namespace MafiaSimulation.AgentFrameworkService;

internal sealed class DeploymentOptions
{
    public bool LocalDevelopment { get; } = ReadBoolean("AGENT_FRAMEWORK_LOCAL_DEV_MODE");
    public bool PersistSessionState => LocalDevelopment;
    public bool LogResponses => LocalDevelopment;
    public int MaxSessions { get; } = ReadPositiveInt("AGENT_FRAMEWORK_MAX_SESSIONS", 20);
    public int MaxCallsPerSession { get; } = ReadPositiveInt("AGENT_FRAMEWORK_MAX_CALLS_PER_SESSION", 300);
    public int MaxEstimatedTokensPerSession { get; } = ReadPositiveInt("AGENT_FRAMEWORK_MAX_TOKENS_PER_SESSION", 120_000);
    public int MaxEstimatedTokensPerDay { get; } = ReadPositiveInt("AGENT_FRAMEWORK_DAILY_TOKEN_BUDGET", 1_000_000);
    public int MaxOutputTokens { get; } = ReadPositiveInt("AGENT_FRAMEWORK_MAX_OUTPUT_TOKENS", 2_048);
    public TimeSpan SessionIdleTimeout { get; } = TimeSpan.FromMinutes(
        ReadPositiveInt("AGENT_FRAMEWORK_SESSION_IDLE_MINUTES", 30));
    public string DataDirectory { get; } = Environment.GetEnvironmentVariable("AGENT_FRAMEWORK_DATA_DIR") is { Length: > 0 } path
        ? Path.GetFullPath(path)
        : Path.Combine(AppContext.BaseDirectory, "data");

    public string? WebRoot
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("AGENT_FRAMEWORK_WEBGL_ROOT");
            string candidate = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
                : Path.GetFullPath(configured);
            return File.Exists(Path.Combine(candidate, "index.html")) ? candidate : null;
        }
    }

    public string? AllowedBrowserOrigin
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("AGENT_FRAMEWORK_ALLOWED_ORIGIN")?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(configured)) return null;
            if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? origin) ||
                origin.Scheme != Uri.UriSchemeHttps ||
                origin.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(origin.Query) ||
                !string.IsNullOrEmpty(origin.Fragment))
                throw new InvalidOperationException("AGENT_FRAMEWORK_ALLOWED_ORIGIN must be an HTTPS origin without a path.");
            return origin.GetLeftPart(UriPartial.Authority);
        }
    }

    private static bool ReadBoolean(string name)
        => string.Equals(Environment.GetEnvironmentVariable(name), "true", StringComparison.OrdinalIgnoreCase)
           || Environment.GetEnvironmentVariable(name) == "1";

    private static int ReadPositiveInt(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0
            ? value
            : fallback;
}
