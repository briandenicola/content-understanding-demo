using Azure.Identity;
using Azure.Storage.Blobs;
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

var app = builder.Build();

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

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
