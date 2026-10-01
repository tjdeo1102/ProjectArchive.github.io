using System.ClientModel;
using System.Net.Sockets;
using System.Text.Json;
using Anthropic;
using Anthropic.Core;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace MafiaSimulation.AgentFrameworkService;

internal static class ProviderFactory
{
    private static readonly HashSet<string> PublicProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "DeepSeek", "Gemini", "Groq", "OpenRouter", "HuggingFace", "Nvidia",
        "Cerebras", "AnthropicClaude", "OpenAI"
    };
    private static readonly IReadOnlyDictionary<string, string> DefaultBaseUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["DeepSeek"] = "https://api.deepseek.com/v1",
        ["OllamaLocal"] = "http://localhost:11434/v1",
        ["Gemini"] = "https://generativelanguage.googleapis.com/v1beta/openai",
        ["Groq"] = "https://api.groq.com/openai/v1",
        ["OpenRouter"] = "https://openrouter.ai/api/v1",
        ["HuggingFace"] = "https://router.huggingface.co/v1",
        ["Nvidia"] = "https://integrate.api.nvidia.com/v1",
        ["Cerebras"] = "https://api.cerebras.ai/v1"
    };

    public static ProviderCollection ReadCollection(string requestJson, DeploymentOptions deployment)
    {
        string json = StripBom(requestJson);
        if (string.IsNullOrWhiteSpace(json) && deployment.LocalDevelopment)
        {
            json = StripBom(Environment.GetEnvironmentVariable("AGENT_FRAMEWORK_PROVIDER_CONFIG_JSON") ?? "");
            string? path = Environment.GetEnvironmentVariable("AGENT_FRAMEWORK_PROVIDER_CONFIG_PATH");
            if (string.IsNullOrWhiteSpace(json) && !string.IsNullOrWhiteSpace(path))
                json = File.ReadAllText(path).TrimStart('\uFEFF');
        }
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Provider configuration is missing for this game session.");
        if (json.Length > 100_000)
            throw new InvalidOperationException("Provider configuration is too large.");
        ProviderCollection collection = JsonSerializer.Deserialize<ProviderCollection>(json, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Provider configuration JSON is invalid.");
        if (collection.Providers is not { Count: > 0 and <= 8 } ||
            collection.Providers.Any(item => item is null || string.IsNullOrWhiteSpace(item.Provider) || string.IsNullOrWhiteSpace(item.ModelName)) ||
            collection.Providers.Select(item => item.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count() != collection.Providers.Count)
            throw new InvalidOperationException("Provider configuration must contain unique providers and model names.");
        if (!deployment.LocalDevelopment && collection.Providers.Any(item =>
                !PublicProviders.Contains(item.Provider) ||
                !string.IsNullOrWhiteSpace(item.CustomBaseUrl) ||
                string.IsNullOrWhiteSpace(item.ApiKey) || item.ApiKey.Length > 8192 ||
                item.ApiKey.Any(char.IsControl) ||
                item.Rpm is < 1 or > 1000 || item.Tpm is < 1 or > 1_000_000 ||
                !ValidModelName(item.ModelName) ||
                item.AllowedModels is { Count: > 16 } ||
                (item.AllowedModels?.Any(model => !ValidModelName(model)) ?? false) ||
                !ValidOptions(item.Options)))
            throw new InvalidOperationException("Public Provider configuration contains an unsupported provider, endpoint, model or limit.");
        // Public NPC assignments must always use the explicitly selected provider/model.
        return deployment.LocalDevelopment ? collection : collection with { ProviderOverride = "" };
    }

    private static bool ValidModelName(string? name)
        => !string.IsNullOrWhiteSpace(name) && name.Length <= 128 &&
           !name.Any(char.IsControl) && !name.Any(char.IsWhiteSpace);

    private static bool ValidOptions(Dictionary<string, JsonElement>? options)
    {
        if (options is null) return true;
        if (options.Count > 3) return false;
        foreach ((string name, JsonElement value) in options)
        {
            if (name is "temperature" or "top_p")
            {
                if (!value.TryGetDouble(out double number) || !double.IsFinite(number) ||
                    number < 0 || number > (name == "top_p" ? 1 : 2)) return false;
            }
            else if (name is "max_tokens" or "max_completion_tokens")
            {
                if (!value.TryGetInt32(out int tokens) || tokens < 1 || tokens > 8192) return false;
            }
            else return false;
        }
        return true;
    }

    public static IChatClient Create(ProviderConfig config, string modelOverride, DeploymentOptions deployment)
    {
        string model = string.IsNullOrWhiteSpace(modelOverride) ? config.ModelName : modelOverride;
        if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException($"Model name is missing for provider {config.Provider}.");
        if (!deployment.LocalDevelopment &&
            !string.Equals(model, config.ModelName, StringComparison.OrdinalIgnoreCase) &&
            !(config.AllowedModels?.Contains(model, StringComparer.OrdinalIgnoreCase) ?? false))
            throw new InvalidOperationException($"Model is not allowed for provider {config.Provider}.");
        if (config.Provider.Equals("AnthropicClaude", StringComparison.OrdinalIgnoreCase))
        {
            var anthropicOptions = new ClientOptions { ApiKey = config.ApiKey };
            if (!string.IsNullOrWhiteSpace(config.CustomBaseUrl)) anthropicOptions.BaseUrl = config.CustomBaseUrl.TrimEnd('/');
            return new AnthropicClient(anthropicOptions).AsIChatClient(model);
        }

        string key = string.IsNullOrWhiteSpace(config.ApiKey) ? "ollama" : config.ApiKey;
        var options = new OpenAIClientOptions();
        string endpoint = ResolveEndpoint(config);
        if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = new Uri(endpoint.TrimEnd('/'));

        return new ChatClient(model, new ApiKeyCredential(key), options).AsIChatClient();
    }

    public static async Task<bool> CanReachAsync(ProviderConfig config, CancellationToken cancellationToken)
    {
        string endpoint = ResolveEndpoint(config);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)) return false;

        int port = uri.IsDefaultPort
            ? uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80
            : uri.Port;
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await client.ConnectAsync(uri.Host, port, timeout.Token);
            return client.Connected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static ChatOptions BuildChatOptions(ProviderConfig config, DeploymentOptions deployment)
    {
        var options = new ChatOptions
        {
            ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<AgentAction>(JsonDefaults.Options, "agent_action", "One Mafia game action"),
            MaxOutputTokens = deployment.MaxOutputTokens
        };
        if (config.Options is null) return options;
        if (TryNumber(config.Options, "temperature", out double temperature)) options.Temperature = (float)temperature;
        if (TryNumber(config.Options, "top_p", out double topP)) options.TopP = (float)topP;
        if (TryInt(config.Options, "max_tokens", out int maxTokens) || TryInt(config.Options, "max_completion_tokens", out maxTokens))
            options.MaxOutputTokens = Math.Clamp(maxTokens, 1, deployment.MaxOutputTokens);
        return options;
    }

    private static bool TryNumber(IReadOnlyDictionary<string, JsonElement> values, string name, out double value)
    {
        value = 0;
        return values.TryGetValue(name, out JsonElement element) && element.TryGetDouble(out value);
    }

    private static bool TryInt(IReadOnlyDictionary<string, JsonElement> values, string name, out int value)
    {
        value = 0;
        return values.TryGetValue(name, out JsonElement element) && element.TryGetInt32(out value);
    }

    private static string ResolveEndpoint(ProviderConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.CustomBaseUrl)) return config.CustomBaseUrl.Trim();
        if (config.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            return "https://api.openai.com/v1";
        if (config.Provider.Equals("AnthropicClaude", StringComparison.OrdinalIgnoreCase))
            return "https://api.anthropic.com";
        return DefaultBaseUrls.GetValueOrDefault(config.Provider, "");
    }

    private static string StripBom(string? value) => (value ?? "").TrimStart('\uFEFF').Trim();
}
