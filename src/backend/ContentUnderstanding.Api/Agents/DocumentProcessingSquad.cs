using Azure.AI.OpenAI;
using Azure.Identity;
using ContentUnderstanding.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ContentUnderstanding.Api.Agents;

/// <summary>
/// Orchestrates a squad of specialized agents for banking document processing.
/// Pipeline: Classifier → Extractor → Compliance → Summary
/// </summary>
public class DocumentProcessingSquad
{
    private readonly IChatClient _chatClient;

    public DocumentProcessingSquad(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:FoundryProjectEndpoint"]
            ?? throw new InvalidOperationException("Azure:FoundryProjectEndpoint is not configured");
        var deploymentName = configuration["Azure:ModelDeploymentName"] ?? "gpt-4o";

        _chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();
    }

    public Workflow BuildWorkflow()
    {
        var classifier = CreateClassifierAgent();
        var extractor = CreateExtractorAgent();
        var compliance = CreateComplianceAgent();
        var summarizer = CreateSummarizerAgent();

        return AgentWorkflowBuilder.BuildSequential([classifier, extractor, compliance, summarizer]);
    }

    public async Task<SquadResult> ProcessAsync(string documentContext)
    {
        var workflow = BuildWorkflow();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, documentContext)
        };

        var results = new List<string>();

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent response)
            {
                results.Add($"[{response.ExecutorId}]: {response.Data}");
            }
            else if (evt is WorkflowOutputEvent output)
            {
                var outputMessages = output.As<List<ChatMessage>>();
                if (outputMessages?.Count > 0)
                {
                    results.Add(outputMessages.Last().Text ?? "");
                }
                break;
            }
            else if (evt is WorkflowErrorEvent error)
            {
                results.Add($"[ERROR]: {error.Exception?.Message ?? "Unknown error"}");
                break;
            }
        }

        return new SquadResult
        {
            AgentOutputs = results,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private ChatClientAgent CreateClassifierAgent() => new(
        _chatClient,
        """
        You are a Document Classification Agent for a banking account opening process.
        Your role is to identify the type of document provided and assess its quality.

        Classify documents into one of these categories:
        - IDENTITY: Passport, driver's license, national ID card
        - PROOF_OF_ADDRESS: Utility bill, bank statement, government letter
        - APPLICATION: Account opening application form
        - UNKNOWN: Cannot determine document type

        Respond with a JSON object:
        {
          "documentType": "<category>",
          "subType": "<specific type e.g. 'passport', 'utility_bill'>",
          "confidence": <0.0-1.0>,
          "qualityIssues": ["<any issues with readability, completeness>"]
        }
        """,
        "ClassifierAgent",
        "Identifies document type and quality for banking KYC");

    private ChatClientAgent CreateExtractorAgent() => new(
        _chatClient,
        """
        You are a Field Extraction Validation Agent for banking KYC.
        You receive document classification and extracted fields from Content Understanding Service.

        Your job is to:
        1. Validate extracted fields match the document type
        2. Flag low-confidence extractions that need human review
        3. Normalize field values (dates to ISO 8601, addresses to standard format)
        4. Identify any additional fields that should have been extracted but weren't

        Respond with a JSON object:
        {
          "validatedFields": [{"name": "", "value": "", "normalized": "", "confidence": 0.0, "needsReview": false}],
          "missingFields": ["<field names that should be present>"],
          "warnings": ["<any data quality concerns>"]
        }
        """,
        "ExtractorAgent",
        "Validates and normalizes extracted document fields");

    private ChatClientAgent CreateComplianceAgent() => new(
        _chatClient,
        """
        You are a KYC Compliance Agent for banking account opening.
        You review extracted and validated document data against regulatory requirements.

        Check for:
        1. Document is not expired
        2. All required KYC fields are present (full name, DOB, address, government ID number)
        3. Address matches between identity doc and proof of address (if both provided)
        4. No sanctions list concerns (flag unusual patterns)
        5. Document is recent enough (proof of address within 3 months)

        Respond with a JSON object:
        {
          "complianceStatus": "PASS" | "FAIL" | "REVIEW_REQUIRED",
          "checks": [{"rule": "", "status": "pass|fail|warning", "detail": ""}],
          "riskScore": <1-10>,
          "recommendation": "<brief recommendation>"
        }
        """,
        "ComplianceAgent",
        "Checks KYC regulatory compliance for account opening");

    private ChatClientAgent CreateSummarizerAgent() => new(
        _chatClient,
        """
        You are a Summary Agent that produces the final account opening assessment.
        You receive outputs from classification, extraction, and compliance agents.

        Produce a clear, concise summary suitable for a bank employee reviewing the application.

        Respond with a JSON object:
        {
          "applicantName": "<full name>",
          "decision": "APPROVE" | "MANUAL_REVIEW" | "REJECT",
          "decisionReason": "<1-2 sentence explanation>",
          "extractedData": {
            "personalInfo": {},
            "address": {},
            "identification": {}
          },
          "nextSteps": ["<action items for the bank employee>"],
          "overallConfidence": <0.0-1.0>
        }
        """,
        "SummarizerAgent",
        "Produces final account opening decision and summary");
}
