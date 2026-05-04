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
        var endpoint = configuration["Azure:ContentUnderstandingEndpoint"]
            ?? throw new InvalidOperationException("Azure:ContentUnderstandingEndpoint is not configured");
        _client = new ContentUnderstandingClient(new Uri(endpoint), credential);
        _logger = logger;
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
}
