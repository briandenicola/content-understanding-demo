using System.Diagnostics;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Core;
using ContentUnderstanding.Api.Models;

namespace ContentUnderstanding.Api.Services;

/// <summary>
/// Service that calls Azure AI Content Understanding using the official SDK.
/// Uses prebuilt analyzers for banking document extraction.
/// </summary>
public class ContentUnderstandingService
{
    private static readonly ActivitySource Activity = new("ContentUnderstanding.Api", "1.0.0");
    private readonly ContentUnderstandingClient _client;
    private readonly ILogger<ContentUnderstandingService> _logger;

    public ContentUnderstandingService(IConfiguration configuration, ILogger<ContentUnderstandingService> logger, TokenCredential credential)
    {
        // CUS SDK requires the Foundry services endpoint (services.ai.azure.com)
        var endpoint = configuration["Azure:ContentUnderstandingEndpoint"]
            ?? throw new InvalidOperationException("Azure:ContentUnderstandingEndpoint is not configured");

        // Convert cognitiveservices.azure.com to services.ai.azure.com if needed
        var uri = new Uri(endpoint);
        if (uri.Host.Contains("cognitiveservices.azure.com"))
        {
            var servicesHost = uri.Host.Replace(".cognitiveservices.azure.com", ".services.ai.azure.com");
            endpoint = $"https://{servicesHost}/";
        }

        _client = new ContentUnderstandingClient(new Uri(endpoint), credential);
        _logger = logger;
        _logger.LogDebug("ContentUnderstandingClient initialized with endpoint: {Endpoint}", endpoint);
    }

    /// <summary>
    /// One-time setup: configure default model deployment mappings for CUS prebuilt analyzers.
    /// This MUST succeed before any analyze calls will work.
    /// </summary>
    public async Task ConfigureDefaultsAsync()
    {
        _logger.LogInformation("Configuring CUS default model deployments...");

        var modelDeployments = new Dictionary<string, string>
        {
            ["gpt-4.1"] = "gpt-4.1",
            ["gpt-4.1-mini"] = "gpt-4.1-mini",
            ["text-embedding-3-large"] = "text-embedding-3-large"
        };

        var response = await _client.UpdateDefaultsAsync(modelDeployments);
        _logger.LogInformation("✅ CUS model defaults configured successfully");

        if (response.Value?.ModelDeployments != null)
        {
            foreach (var kvp in response.Value.ModelDeployments)
            {
                _logger.LogInformation("  {Model} → {Deployment}", kvp.Key, kvp.Value);
            }
        }
    }

    public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(string blobUrl)
    {
        using var span = Activity.StartActivity("AnalyzeDocument");
        span?.SetTag("document.blob_url", blobUrl);

        var analyzerName = "prebuilt-documentSearch";
        _logger.LogInformation("Analyzing document with {Analyzer}: {BlobUrl}", analyzerName, blobUrl);

        var operation = await _client.AnalyzeAsync(
            WaitUntil.Completed,
            analyzerName,
            inputs: [new AnalysisInput { Uri = new Uri(blobUrl) }]);

        var result = operation.Value;
        _logger.LogInformation("Analysis complete: {ContentCount} content item(s)", result.Contents?.Count ?? 0);

        var fields = new List<ExtractedField>();
        var markdownContent = string.Empty;

        if (result.Contents != null)
        {
            foreach (var content in result.Contents)
            {
                markdownContent += content.Markdown + "\n";
                _logger.LogDebug("Content item: {Length} chars markdown", content.Markdown?.Length ?? 0);
            }
        }

        return new DocumentAnalysisResult
        {
            DocumentId = operation.Id ?? Guid.NewGuid().ToString(),
            DocumentType = DetectDocumentType(markdownContent),
            FileName = blobUrl.Split('/').Last(),
            AnalyzedAt = DateTime.UtcNow,
            OverallConfidence = 0.95,
            Markdown = markdownContent,
            Fields = fields
        };
    }

    public async Task<DocumentAnalysisResult> AnalyzeDocumentWithFieldsAsync(string blobUrl, string analyzerName)
    {
        using var span = Activity.StartActivity("AnalyzeDocumentWithFields");
        span?.SetTag("document.blob_url", blobUrl);
        span?.SetTag("cus.analyzer", analyzerName);

        _logger.LogInformation("Analyzing document with {Analyzer}: {BlobUrl}", analyzerName, blobUrl);

        var operation = await _client.AnalyzeAsync(
            WaitUntil.Completed,
            analyzerName,
            inputs: [new AnalysisInput { Uri = new Uri(blobUrl) }]);

        var result = operation.Value;
        _logger.LogInformation("Analysis complete: {ContentCount} content item(s)", result.Contents?.Count ?? 0);

        var fields = new List<ExtractedField>();
        var markdownContent = string.Empty;

        if (result.Contents != null)
        {
            foreach (var content in result.Contents)
            {
                markdownContent += content.Markdown + "\n";

                if (content.Fields != null)
                {
                    foreach (var field in content.Fields)
                    {
                        var fieldValue = field.Value switch
                        {
                            ContentStringField sf => sf.Value ?? "",
                            ContentNumberField nf => nf.Value?.ToString() ?? "",
                            ContentDateTimeOffsetField df => df.Value?.ToString("yyyy-MM-dd") ?? "",
                            _ => field.Value?.ToString() ?? ""
                        };

                        var extractedField = new ExtractedField
                        {
                            Name = field.Key,
                            Value = fieldValue,
                            Confidence = field.Value?.Confidence ?? 0.0,
                            Category = DetermineCategory(field.Key)
                        };
                        fields.Add(extractedField);
                        _logger.LogDebug("  Field: {Name} = {Value} ({Confidence:P0})",
                            extractedField.Name, extractedField.Value, extractedField.Confidence);
                    }
                }
            }
        }

        return new DocumentAnalysisResult
        {
            DocumentId = operation.Id ?? Guid.NewGuid().ToString(),
            DocumentType = DetectDocumentType(markdownContent),
            FileName = blobUrl.Split('/').Last(),
            AnalyzedAt = DateTime.UtcNow,
            OverallConfidence = fields.Count > 0 ? fields.Average(f => f.Confidence) : 0.95,
            Markdown = markdownContent,
            Fields = fields
        };
    }

    private static string DetectDocumentType(string markdown)
    {
        var lower = markdown.ToLowerInvariant();
        if (lower.Contains("driver") && lower.Contains("license"))
            return "Driver's License";
        if (lower.Contains("passport"))
            return "Passport";
        if (lower.Contains("utility") || lower.Contains("kwh") || lower.Contains("meter"))
            return "Utility Bill";
        if (lower.Contains("account application") || lower.Contains("new account"))
            return "Account Application";
        if (lower.Contains("statement") && lower.Contains("balance"))
            return "Bank Statement";
        if (lower.Contains("w-2") || lower.Contains("wage and tax"))
            return "W-2 Tax Form";
        return "Unknown Document";
    }

    private static string DetermineCategory(string fieldName) => fieldName.ToLowerInvariant() switch
    {
        "firstname" or "lastname" or "fullname" or "dateofbirth" or "name" => "Personal Information",
        "address" or "city" or "state" or "zipcode" or "country" or "street" => "Address",
        "documentnumber" or "passportnumber" or "licensenumber" => "Document ID",
        "expirationdate" or "issuedate" or "statementdate" => "Dates",
        "amount" or "balance" or "income" or "wages" => "Financial",
        _ => "Other"
    };

    /// <summary>
    /// Analyze a document from binary data (direct upload, no blob storage needed).
    /// </summary>
    public async Task<DocumentAnalysisResult> AnalyzeBinaryAsync(string fileName, byte[] fileContent)
    {
        using var span = Activity.StartActivity("AnalyzeBinary");
        span?.SetTag("document.filename", fileName);
        span?.SetTag("document.size_bytes", fileContent.Length);

        var analyzerName = "prebuilt-documentSearch";
        _logger.LogInformation("Analyzing binary document with {Analyzer}: {FileName} ({Size} bytes)",
            analyzerName, fileName, fileContent.Length);

        var binaryData = BinaryData.FromBytes(fileContent);
        var operation = await _client.AnalyzeBinaryAsync(
            WaitUntil.Completed,
            analyzerName,
            binaryData);

        var result = operation.Value;
        _logger.LogInformation("Binary analysis complete: {ContentCount} content item(s)", result.Contents?.Count ?? 0);

        var fields = new List<ExtractedField>();
        var markdownContent = string.Empty;

        if (result.Contents != null)
        {
            foreach (var content in result.Contents)
            {
                markdownContent += content.Markdown + "\n";

                if (content.Fields != null)
                {
                    foreach (var field in content.Fields)
                    {
                        var fieldValue = field.Value switch
                        {
                            ContentStringField sf => sf.Value ?? "",
                            ContentNumberField nf => nf.Value?.ToString() ?? "",
                            ContentDateTimeOffsetField df => df.Value?.ToString("yyyy-MM-dd") ?? "",
                            _ => field.Value?.ToString() ?? ""
                        };

                        fields.Add(new ExtractedField
                        {
                            Name = field.Key,
                            Value = fieldValue,
                            Confidence = field.Value?.Confidence ?? 0.0,
                            ConfidenceLabel = ConfidenceLabelFor(field.Value?.Confidence ?? 0.0),
                            Category = DetermineCategory(field.Key)
                        });
                    }
                }
            }
        }

        return new DocumentAnalysisResult
        {
            DocumentId = operation.Id ?? Guid.NewGuid().ToString(),
            DocumentType = DetectDocumentType(markdownContent),
            FileName = fileName,
            AnalyzedAt = DateTime.UtcNow,
            OverallConfidence = ComputeExtractionConfidence(markdownContent, fields),
            ConfidenceExplanation = BuildConfidenceExplanation(markdownContent, fields),
            Markdown = markdownContent,
            Fields = fields
        };
    }

    // Required fields for bank account opening
    private static readonly (string Name, string[] Patterns)[] AccountOpeningFields =
    [
        ("Full Name", ["name", "full name", "first name", "last name", "applicant"]),
        ("Date of Birth", ["date of birth", "dob", "birth date", "born"]),
        ("Address", ["address", "street", "city", "state", "zip", "postal"]),
        ("ID Number", ["id number", "license number", "passport number", "ssn", "social security", "document number"]),
        ("ID Expiration", ["expir", "valid until", "valid through", "exp date"])
    ];

    private static (int found, int total, List<string> matched, List<string> missing) CheckAccountOpeningFields(string markdown, List<ExtractedField> fields)
    {
        var contentLower = (markdown ?? "").ToLowerInvariant();
        var fieldValues = fields.Select(f => $"{f.Name} {f.Value}".ToLowerInvariant()).ToList();
        var allText = contentLower + " " + string.Join(" ", fieldValues);

        var matched = new List<string>();
        var missing = new List<string>();

        foreach (var (name, patterns) in AccountOpeningFields)
        {
            if (patterns.Any(p => allText.Contains(p)))
                matched.Add(name);
            else
                missing.Add(name);
        }

        return (matched.Count, AccountOpeningFields.Length, matched, missing);
    }

    private static double ComputeExtractionConfidence(string markdown, List<ExtractedField> fields)
    {
        if (string.IsNullOrWhiteSpace(markdown) && fields.Count == 0)
            return 0.0;

        var (found, total, _, _) = CheckAccountOpeningFields(markdown, fields);

        // Score is proportion of required account-opening fields detected
        // with a small bonus for extraction quality
        var fieldScore = (double)found / total;

        // Small bonus for rich content (up to +0.1)
        var qualityBonus = 0.0;
        if (!string.IsNullOrWhiteSpace(markdown))
        {
            if (markdown.Length > 500) qualityBonus = 0.1;
            else if (markdown.Length > 200) qualityBonus = 0.05;
        }

        return Math.Min(fieldScore * 0.9 + qualityBonus, 1.0);
    }

    private static string BuildConfidenceExplanation(string markdown, List<ExtractedField> fields)
    {
        if (string.IsNullOrWhiteSpace(markdown) && fields.Count == 0)
            return "No content could be extracted from this document.";

        var (found, total, matched, missing) = CheckAccountOpeningFields(markdown, fields);

        var explanation = $"{found} of {total} required account-opening fields detected";

        if (matched.Count > 0)
            explanation += $" (found: {string.Join(", ", matched)})";

        if (missing.Count > 0)
            explanation += $". Missing: {string.Join(", ", missing)}";

        return explanation + ".";
    }

    private static string ConfidenceLabelFor(double confidence) => confidence switch
    {
        >= 0.95 => "Very High",
        >= 0.85 => "High",
        >= 0.70 => "Medium",
        >= 0.50 => "Low",
        _ => "Very Low"
    };
}
