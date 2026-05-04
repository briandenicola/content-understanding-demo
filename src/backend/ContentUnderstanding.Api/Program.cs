using Azure.Identity;
using Azure.Storage.Blobs;
using ContentUnderstanding.Api.Agents;
using ContentUnderstanding.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton(_ =>
{
    var connectionString = builder.Configuration["Azure:StorageConnectionString"];
    return new BlobServiceClient(connectionString);
});

builder.Services.AddSingleton<ContentUnderstandingService>();
builder.Services.AddSingleton<DocumentAgentService>();
builder.Services.AddSingleton<DocumentProcessingSquad>();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapPost("/api/documents/upload", async (HttpRequest request, ContentUnderstandingService cus, BlobServiceClient blobClient) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file provided");

    var containerClient = blobClient.GetBlobContainerClient("document-uploads");
    await containerClient.CreateIfNotExistsAsync();

    var blobName = $"{Guid.NewGuid()}/{file.FileName}";
    var blobClientInstance = containerClient.GetBlobClient(blobName);

    await using var stream = file.OpenReadStream();
    await blobClientInstance.UploadAsync(stream, overwrite: true);

    var result = await cus.AnalyzeDocumentAsync(blobClientInstance.Uri.ToString());
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

app.MapPost("/api/documents/squad-process", async (HttpRequest request, DocumentProcessingSquad squad, ContentUnderstandingService cus, BlobServiceClient blobClient) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file provided");

    var containerClient = blobClient.GetBlobContainerClient("document-uploads");
    await containerClient.CreateIfNotExistsAsync();

    var blobName = $"{Guid.NewGuid()}/{file.FileName}";
    var blobClientInstance = containerClient.GetBlobClient(blobName);

    await using var stream = file.OpenReadStream();
    await blobClientInstance.UploadAsync(stream, overwrite: true);

    // Run CUS extraction first
    var cusResult = await cus.AnalyzeDocumentAsync(blobClientInstance.Uri.ToString());

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
