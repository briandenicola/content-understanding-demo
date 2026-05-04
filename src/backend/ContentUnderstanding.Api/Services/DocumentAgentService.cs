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

        // AIProjectClient needs the OpenAI endpoint for model completions
        // The Agent Framework uses the OpenAI Responses API internally
        var endpoint = _configuration["Azure:FoundryProjectEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Azure:FoundryProjectEndpoint is not configured. Run 'task up' to deploy infrastructure.");

        // Extract the OpenAI endpoint from the Foundry services URL
        // Format: https://<name>.openai.azure.com/
        var endpointUri = new Uri(endpoint);
        var openAiEndpoint = new Uri($"https://{endpointUri.Host.Replace(".services.ai.azure.com", ".openai.azure.com")}/");

        var modelDeployment = _configuration["Azure:ModelDeploymentName"] ?? "gpt-4.1";
        _logger.LogDebug("Using model deployment: {Model} at endpoint: {Endpoint}", modelDeployment, openAiEndpoint);
        span?.SetTag("ai.model", modelDeployment);
        span?.SetTag("ai.endpoint", openAiEndpoint.ToString());

        var projectClient = new AIProjectClient(openAiEndpoint, _credential);

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

        // Run CUS analysis first (binary upload, no blob storage needed)
        _logger.LogDebug("Running CUS binary analysis...");
        var cusResult = await _cusService.AnalyzeBinaryAsync(fileName, fileContent);
        _logger.LogDebug("CUS extraction: {FieldCount} fields for {DocType}",
            cusResult.Fields.Count, cusResult.DocumentType);

        var fieldsDescription = cusResult.Fields.Count > 0
            ? string.Join("\n", cusResult.Fields.Select(f =>
                $"- {f.Name}: \"{f.Value}\" (confidence: {f.Confidence:P0})"))
            : $"No structured fields extracted. Document content:\n{cusResult.Markdown?[..Math.Min(cusResult.Markdown.Length, 2000)]}";

        _logger.LogDebug("Sending to agent for review...");
        var agentResponse = await agent.RunAsync(
            $"Review this document extraction for account opening eligibility:\n\nDocument: {fileName}\nType: {cusResult.DocumentType}\n\nExtracted fields:\n{fieldsDescription}");

        _logger.LogInformation("Agent response received ({Length} chars)", agentResponse?.ToString()?.Length ?? 0);
        _logger.LogDebug("Agent response: {Response}", agentResponse);

        return cusResult with
        {
            AgentSummary = agentResponse?.ToString()
        };
    }

}
