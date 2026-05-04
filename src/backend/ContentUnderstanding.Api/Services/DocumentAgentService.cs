using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Core;
using ContentUnderstanding.Api.Models;

namespace ContentUnderstanding.Api.Services;

/// <summary>
/// Uses Microsoft Agent Framework to provide intelligent document processing with AI agent orchestration.
/// The agent reviews CUS results and provides a structured summary suitable for account opening.
/// </summary>
public class DocumentAgentService
{
    private static readonly ActivitySource Activity = new("ContentUnderstanding.Api", "1.0.0");
    private readonly IConfiguration _configuration;
    private readonly ContentUnderstandingService _cusService;
    private readonly TokenCredential _credential;
    private readonly ILogger<DocumentAgentService> _logger;

    public DocumentAgentService(IConfiguration configuration, ContentUnderstandingService cusService, TokenCredential credential, ILogger<DocumentAgentService> logger)
    {
        _configuration = configuration;
        _cusService = cusService;
        _credential = credential;
        _logger = logger;
    }

    public async Task<DocumentAnalysisResult> ProcessDocumentAsync(string fileName, byte[] fileContent)
    {
        using var span = Activity.StartActivity("ProcessDocumentWithAgent");
        span?.SetTag("document.file_name", fileName);
        span?.SetTag("document.size_bytes", fileContent.Length);

        _logger.LogInformation("Processing document with agent: {FileName} ({Size} bytes)", fileName, fileContent.Length);

        // Use the CUS endpoint (hub-level) for AIProjectClient — model deployments live here
        var endpoint = _configuration["Azure:ContentUnderstandingEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Azure:ContentUnderstandingEndpoint is not configured. Run 'task up' to deploy infrastructure.");

        var modelDeployment = _configuration["Azure:ModelDeploymentName"] ?? "gpt-5.4";
        _logger.LogDebug("Using model deployment: {Model} at endpoint: {Endpoint}", modelDeployment, endpoint);
        span?.SetTag("ai.model", modelDeployment);
        span?.SetTag("ai.endpoint", endpoint);

        var projectClient = new AIProjectClient(new Uri(endpoint), _credential);

        var agent = projectClient.AsAIAgent(
            model: modelDeployment,
            name: "DocumentReviewAgent",
            instructions: """
                You are a banking document review agent. Your job is to:
                1. Review extracted document content from Content Understanding Service
                2. Identify the document type (ID, proof of address, application form, etc.)
                3. Extract key fields relevant for account opening
                4. Validate completeness and flag any issues

                Required fields for account opening:
                - Full name (first + last)
                - Date of birth
                - Address (street, city, state/province, postal code)
                - Government-issued ID number
                - ID expiration date (must not be expired)

                Respond with a JSON object containing:
                - "documentType": detected type
                - "summary": brief assessment
                - "isComplete": boolean indicating if all required fields are present
                - "extractedFields": object with key-value pairs of extracted data
                - "missingFields": array of missing required field names
                - "flags": array of any concerns (expired docs, low confidence, etc.)
                """);

        // For now, create a simulated CUS result (until blob upload pipeline is wired)
        var simulatedResult = CreateSimulatedResult(fileName);
        _logger.LogDebug("Simulated CUS extraction: {FieldCount} fields for {DocType}",
            simulatedResult.Fields.Count, simulatedResult.DocumentType);

        var fieldsDescription = string.Join("\n", simulatedResult.Fields.Select(f =>
            $"- {f.Name}: \"{f.Value}\" (confidence: {f.Confidence:P0})"));

        _logger.LogDebug("Sending to agent for review...");
        var agentResponse = await agent.RunAsync(
            $"Review this document extraction for account opening eligibility:\n\nDocument: {fileName}\nType: {simulatedResult.DocumentType}\n\nExtracted fields:\n{fieldsDescription}");

        _logger.LogInformation("Agent response received ({Length} chars)", agentResponse?.ToString()?.Length ?? 0);
        _logger.LogDebug("Agent response: {Response}", agentResponse);

        return simulatedResult with
        {
            AgentSummary = agentResponse?.ToString()
        };
    }

    private static DocumentAnalysisResult CreateSimulatedResult(string fileName)
    {
        return new DocumentAnalysisResult
        {
            DocumentId = Guid.NewGuid().ToString(),
            DocumentType = "Identity Document",
            FileName = fileName,
            AnalyzedAt = DateTime.UtcNow,
            OverallConfidence = 0.94,
            Fields =
            [
                new() { Name = "FirstName", Value = "John", Confidence = 0.98, Category = "Personal Information" },
                new() { Name = "LastName", Value = "Smith", Confidence = 0.97, Category = "Personal Information" },
                new() { Name = "DateOfBirth", Value = "1985-03-15", Confidence = 0.95, Category = "Personal Information" },
                new() { Name = "Address", Value = "123 Main Street", Confidence = 0.92, Category = "Address" },
                new() { Name = "City", Value = "Seattle", Confidence = 0.96, Category = "Address" },
                new() { Name = "State", Value = "WA", Confidence = 0.98, Category = "Address" },
                new() { Name = "ZipCode", Value = "98101", Confidence = 0.94, Category = "Address" },
                new() { Name = "DocumentNumber", Value = "DL-987654321", Confidence = 0.91, Category = "Document ID" },
                new() { Name = "ExpirationDate", Value = "2028-06-30", Confidence = 0.89, Category = "Dates" },
                new() { Name = "IssueDate", Value = "2020-06-30", Confidence = 0.88, Category = "Dates" },
            ]
        };
    }
}
