using MafiaSimulation.AgentFrameworkService;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var deployment = new DeploymentOptions();
string? allowedOrigin = deployment.AllowedBrowserOrigin;
if (deployment.LocalDevelopment && deployment.WebRoot != null)
    throw new InvalidOperationException("A public WebGL build cannot run with local development credentials enabled.");

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonDefaults.Options.PropertyNamingPolicy;
    options.SerializerOptions.DictionaryKeyPolicy = JsonDefaults.Options.DictionaryKeyPolicy;
});
builder.Services.AddSingleton(deployment);
if (allowedOrigin != null)
    builder.Services.AddCors(options => options.AddPolicy("portfolio", policy => policy
        .WithOrigins(allowedOrigin)
        .WithMethods("GET", "POST", "DELETE")
        .WithHeaders("Content-Type", "X-Agent-Session-Token")));
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("session-create", limiter =>
{
    limiter.PermitLimit = 12;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
    limiter.AutoReplenishment = true;
}));
builder.Services.Configure<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>(options =>
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("provider-validate", limiter =>
{
    limiter.PermitLimit = 4;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
    limiter.AutoReplenishment = true;
}));

WebApplication app = builder.Build();
if (allowedOrigin != null) app.UseCors("portfolio");
app.UseRateLimiter();

RouteGroupBuilder routes;
if (deployment.WebRoot is { } webRoot)
{
    var provider = new PhysicalFileProvider(webRoot);
    var contentTypes = new FileExtensionContentTypeProvider();
    contentTypes.Mappings[".unityweb"] = "application/octet-stream";
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, ContentTypeProvider = contentTypes });
    routes = app.MapGroup("/agent-framework");
}
else
{
    routes = app.MapGroup(deployment.LocalDevelopment ? string.Empty : "/agent-framework");
}

routes.MapGet("/health", () => Results.Ok(new { status = "ok", framework = "Microsoft Agent Framework for .NET" }));

routes.MapPost("/providers/validate", async (ProviderValidationRequest request, HttpContext context, CancellationToken cancellationToken) =>
{
    context.Response.Headers.CacheControl = "no-store";
    try
    {
        ProviderCollection collection = ProviderFactory.ReadCollection(request.ProviderConfigJson, deployment);
        var candidates = collection.Providers
            .SelectMany(provider => new[] { provider.ModelName }.Concat(provider.AllowedModels ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(model => (provider, model)))
            .ToList();
        if (candidates.Count > 8)
            return Results.BadRequest(new { detail = "Upload at most eight models for validation." });

        var verified = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (provider, model) in candidates)
        {
            IChatClient client = ProviderFactory.Create(provider, model, deployment);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                await client.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "Reply OK.")],
                    new ChatOptions { MaxOutputTokens = 8 }, timeout.Token);
                if (!verified.TryGetValue(provider.Provider, out List<string>? models))
                    verified[provider.Provider] = models = [];
                models.Add(model);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Do not expose provider errors or credentials to the browser or server logs.
            }
            finally
            {
                if (client is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
                else if (client is IDisposable disposable) disposable.Dispose();
            }
        }
        if (verified.Count == 0)
            return Results.BadRequest(new { detail = "No models accepted a test request. Check API keys, model names and provider quotas." });
        return Results.Ok(new ProviderValidationResponse(verified
            .Select(item => new ProviderModelList(item.Key, item.Value)).ToList()));
    }
    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or NotSupportedException)
    {
        return Results.BadRequest(new { detail = "Invalid Provider configuration. Check credentials, models and limits." });
    }
}).RequireRateLimiting("provider-validate");

routes.MapPost("/sessions", async (CreateSessionRequest request, HttpContext context, SessionRegistry registry, CancellationToken token) =>
{
    context.Response.Headers.CacheControl = "no-store";
    try
    {
        SessionRequestValidator.Validate(request, deployment);
        RegisteredSession registration = await registry.CreateAsync(request, token);
        return Results.Ok(new SessionResponse(
            registration.Session.SessionId,
            registration.Token,
            registration.Session.Agents.Keys.ToList(),
            registration.Session.ProviderStatuses));
    }
    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or NotSupportedException)
    {
        if (deployment.LocalDevelopment) app.Logger.LogWarning(exception, "Session creation rejected.");
        else app.Logger.LogWarning("Session creation rejected: {ErrorType}", exception.GetType().Name);
        return Results.BadRequest(new { detail = deployment.LocalDevelopment ? exception.Message : "Invalid or unavailable game settings." });
    }
}).RequireRateLimiting("session-create");

routes.MapDelete("/sessions/{sessionId}", (string sessionId, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async _ =>
    {
        await registry.RemoveAsync(sessionId, GetToken(context));
        return Results.NoContent();
    }));

routes.MapPost("/sessions/{sessionId}/close", async (string sessionId, HttpContext context, SessionRegistry registry) =>
{
    if (context.Request.ContentLength is > 128) return Results.BadRequest();
    using var reader = new StreamReader(context.Request.Body);
    char[] buffer = new char[129];
    int length = await reader.ReadBlockAsync(buffer.AsMemory(), context.RequestAborted);
    if (length is < 1 or > 128) return Results.BadRequest();
    string token = new(buffer, 0, length);
    try
    {
        await registry.RemoveAsync(sessionId, token);
        return Results.NoContent();
    }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
});

routes.MapPost("/sessions/{sessionId}/heartbeat", (string sessionId, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, _ => Task.FromResult<IResult>(Results.NoContent())));

routes.MapPost("/sessions/{sessionId}/actions", (string sessionId, AgentActionRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        return Results.Ok(await session.RunAgentActionAsync(request, context.RequestAborted));
    }));

routes.MapPost("/sessions/{sessionId}/meeting", (string sessionId, MeetingRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        return Results.Ok(new ActionListResponse(await session.RunMeetingAsync(request, context.RequestAborted)));
    }));

routes.MapPost("/sessions/{sessionId}/dialogue", (string sessionId, DialogueRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        return Results.Ok(new ActionListResponse(await session.RunDialogueAsync(request, context.RequestAborted)));
    }));

routes.MapGet("/sessions/{sessionId}/operations/{operationId}/progress", (
    string sessionId,
    string operationId,
    int offset,
    HttpContext context,
    SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, session =>
        Task.FromResult<IResult>(Results.Ok(session.GetOperationProgress(operationId, offset)))));

routes.MapPost("/sessions/{sessionId}/voting", (string sessionId, PhaseDecisionRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        return Results.Ok(new ActionListResponse(await session.RunPhaseAsync(request, "voting", context.RequestAborted)));
    }));

routes.MapPost("/sessions/{sessionId}/night", (string sessionId, PhaseDecisionRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        return Results.Ok(new ActionListResponse(await session.RunPhaseAsync(request, "night", context.RequestAborted)));
    }));

routes.MapPost("/sessions/{sessionId}/events", (string sessionId, GameEventRequest request, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, async session =>
    {
        SessionRequestValidator.Validate(request);
        await session.RecordEventAsync(request, context.RequestAborted);
        return Results.NoContent();
    }));

routes.MapPost("/sessions/{sessionId}/operations/{operationId}/cancel", (string sessionId, string operationId, HttpContext context, SessionRegistry registry) =>
    WithSessionAsync(sessionId, context, registry, session =>
        Task.FromResult<IResult>(Results.Ok(new { cancelled = session.Cancel(operationId) }))));

if (deployment.LocalDevelopment)
{
    routes.MapGet("/sessions/{sessionId}/state", (string sessionId, HttpContext context, SessionRegistry registry) =>
        WithSessionAsync(sessionId, context, registry, session =>
            Task.FromResult<IResult>(Results.Ok(new SessionStateResponse(sessionId, session.ExportState())))));

    routes.MapPut("/sessions/{sessionId}/state", (string sessionId, LoadSessionStateRequest request, HttpContext context, SessionRegistry registry) =>
        WithSessionAsync(sessionId, context, registry, async session =>
        {
            await session.LoadStateAsync(request.State, context.RequestAborted);
            return Results.NoContent();
        }));
}

await app.RunAsync();

static string? GetToken(HttpContext context)
    => context.Request.Headers["X-Agent-Session-Token"].ToString();

static async Task<IResult> WithSessionAsync(
    string sessionId,
    HttpContext context,
    SessionRegistry registry,
    Func<GameSession, Task<IResult>> action)
{
    try { return await action(registry.Get(sessionId, GetToken(context))); }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (ArgumentException) { return Results.BadRequest(new { detail = "Invalid game request." }); }
    catch (OperationCanceledException) { return Results.StatusCode(StatusCodes.Status499ClientClosedRequest); }
    catch (Exception exception)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AgentFrameworkApi");
        if (context.RequestServices.GetRequiredService<DeploymentOptions>().LocalDevelopment)
            logger.LogError(exception, "Game operation failed.");
        else
            logger.LogError("Game operation failed: {ErrorType}", exception.GetType().Name);
        return Results.Json(new { detail = "Game operation failed. Please retry or restart the game." },
            statusCode: StatusCodes.Status502BadGateway);
    }
}
