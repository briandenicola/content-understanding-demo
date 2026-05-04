using System.Diagnostics;
using Azure.Identity;
using ContentUnderstanding.Api.Agents;
using ContentUnderstanding.Api.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "ContentUnderstanding.Api";
const string serviceVersion = "1.0.0";

// Configure logging with OTEL + console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion: serviceVersion));
    options.AddConsoleExporter();
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddFilter("ContentUnderstanding", LogLevel.Trace);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Debug);
    builder.Logging.AddFilter("System.Net.Http", LogLevel.Debug);
}

// OpenTelemetry tracing & metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddSource(serviceName)
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(serviceName)
            .AddConsoleExporter();
    });

// Azure Monitor (App Insights) if connection string is available
var appInsightsConn = builder.Configuration["Azure:ApplicationInsightsConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConn))
    builder.Services.AddOpenTelemetry().UseAzureMonitor(o => o.ConnectionString = appInsightsConn);

// Application activity source for custom spans
builder.Services.AddSingleton(new ActivitySource(serviceName, serviceVersion));

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton<ContentUnderstandingService>();
builder.Services.AddSingleton<DocumentAgentService>();
builder.Services.AddSingleton<DocumentProcessingSquad>();

// Entra ID credential chain: Azure CLI → Environment → Managed Identity
var credential = new ChainedTokenCredential(
    new AzureCliCredential(),
    new EnvironmentCredential(),
    new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned));
builder.Services.AddSingleton<Azure.Core.TokenCredential>(credential);

var app = builder.Build();

// Log configuration status at startup
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var cusEndpoint = app.Configuration["Azure:ContentUnderstandingEndpoint"];
var foundryEndpoint = app.Configuration["Azure:FoundryProjectEndpoint"];
var storageConn = app.Configuration["Azure:StorageConnectionString"];

logger.LogInformation("=== Content Understanding Demo ===");
logger.LogInformation("CUS Endpoint:     {Endpoint}", string.IsNullOrEmpty(cusEndpoint) ? "⚠️  NOT CONFIGURED" : cusEndpoint);
logger.LogInformation("Foundry Project:  {Endpoint}", string.IsNullOrEmpty(foundryEndpoint) ? "⚠️  NOT CONFIGURED" : foundryEndpoint);
logger.LogInformation("Storage Account:  {Status}", string.IsNullOrEmpty(storageConn) ? "⚠️  NOT CONFIGURED" : "✅ Configured");
logger.LogInformation("Auth chain:       AzureCliCredential → EnvironmentCredential → ManagedIdentityCredential");

// Connectivity & RBAC validation at startup
if (!string.IsNullOrEmpty(foundryEndpoint))
{
    logger.LogInformation("Verifying Entra ID token acquisition...");
    try
    {
        var tokenRequest = new Azure.Core.TokenRequestContext(["https://cognitiveservices.azure.com/.default"]);
        var token = await credential.GetTokenAsync(tokenRequest);
        logger.LogInformation("✅ Entra ID token acquired successfully (expires {ExpiresOn})", token.ExpiresOn);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ Entra ID token acquisition FAILED: {Message}", ex.Message);
        throw;
    }
}
else
{
    logger.LogWarning("⚠️  Skipping connectivity check — Foundry endpoint not configured");
}

// Configure CUS model defaults (one-time setup, idempotent)
if (!string.IsNullOrEmpty(cusEndpoint))
{
    var cusService = app.Services.GetRequiredService<ContentUnderstandingService>();
    await cusService.ConfigureDefaultsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapPost("/api/documents/upload", async (HttpRequest request, ContentUnderstandingService cus) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file provided");

    await using var stream = file.OpenReadStream();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    var fileBytes = ms.ToArray();

    var result = await cus.AnalyzeBinaryAsync(file.FileName, fileBytes);
    return Results.Ok(result);
})
.WithName("UploadDocument")
.DisableAntiforgery();

app.MapPost("/api/documents/analyze-agent", async (HttpRequest request, DocumentAgentService agentService) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file provided");

    await using var stream = file.OpenReadStream();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    var fileBytes = ms.ToArray();

    var result = await agentService.ProcessDocumentAsync(file.FileName, fileBytes);
    return Results.Ok(result);
})
.WithName("AnalyzeWithAgent")
.DisableAntiforgery();

app.MapPost("/api/documents/squad-process", async (HttpRequest request, DocumentProcessingSquad squad, ContentUnderstandingService cus) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file provided");

    await using var stream = file.OpenReadStream();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    var fileBytes = ms.ToArray();

    // Run CUS extraction first (binary upload, no blob storage needed)
    var cusResult = await cus.AnalyzeBinaryAsync(file.FileName, fileBytes);

    // Feed CUS results into the agent squad pipeline
    var documentContext = $"""
        Document: {file.FileName}
        Type detected by CUS: {cusResult.DocumentType}
        Overall CUS confidence: {cusResult.OverallConfidence:P0}

        Extracted fields:
        {string.Join("\n", cusResult.Fields.Select(f => $"- {f.Name} ({f.Category}): \"{f.Value}\" [confidence: {f.Confidence:P0}]"))}
        """;

    var squadResult = await squad.ProcessAsync(documentContext);

    return Results.Ok(new
    {
        cusResult,
        squadResult
    });
})
.WithName("SquadProcess")
.DisableAntiforgery();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
