using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContentUnderstanding.Api.Models;

namespace ContentUnderstanding.Api.Services;

/// <summary>
/// Service that calls Azure AI Content Understanding to analyze uploaded documents.
/// </summary>
public class ContentUnderstandingService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public ContentUnderstandingService(IConfiguration configuration)
    {
        _endpoint = configuration["Azure:ContentUnderstandingEndpoint"]
            ?? throw new InvalidOperationException("Azure:ContentUnderstandingEndpoint is not configured");
        _apiKey = configuration["Azure:ContentUnderstandingKey"]
            ?? throw new InvalidOperationException("Azure:ContentUnderstandingKey is not configured");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
    }

    public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(string blobUrl)
    {
        var analyzerName = "banking-document-analyzer";
        var requestUrl = $"{_endpoint.TrimEnd('/')}/contentunderstanding/analyzers/{analyzerName}:analyze?api-version=2024-12-01-preview";

        var requestBody = new
        {
            url = blobUrl
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(requestUrl, content);
        response.EnsureSuccessStatusCode();

        var operationLocation = response.Headers.GetValues("Operation-Location").First();

        // Poll for completion
        return await PollForResultAsync(operationLocation);
    }

    private async Task<DocumentAnalysisResult> PollForResultAsync(string operationUrl)
    {
        while (true)
        {
            await Task.Delay(2000);

            var response = await _httpClient.GetAsync(operationUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status == "succeeded")
            {
                return ParseAnalysisResult(root);
            }
            else if (status == "failed")
            {
                var error = root.TryGetProperty("error", out var errProp)
                    ? errProp.GetProperty("message").GetString()
                    : "Unknown error";
                throw new InvalidOperationException($"Analysis failed: {error}");
            }
        }
    }

    private static DocumentAnalysisResult ParseAnalysisResult(JsonElement root)
    {
        var result = root.GetProperty("result");
        var fields = new List<ExtractedField>();

        if (result.TryGetProperty("contents", out var contents))
        {
            foreach (var contentItem in contents.EnumerateArray())
            {
                if (contentItem.TryGetProperty("fields", out var fieldsProp))
                {
                    foreach (var field in fieldsProp.EnumerateObject())
                    {
                        var value = field.Value.TryGetProperty("valueString", out var vs)
                            ? vs.GetString() ?? ""
                            : field.Value.TryGetProperty("content", out var c)
                                ? c.GetString() ?? ""
                                : "";

                        var confidence = field.Value.TryGetProperty("confidence", out var conf)
                            ? conf.GetDouble()
                            : 0.0;

                        fields.Add(new ExtractedField
                        {
                            Name = field.Name,
                            Value = value,
                            Confidence = confidence,
                            Category = DetermineCategory(field.Name)
                        });
                    }
                }
            }
        }

        return new DocumentAnalysisResult
        {
            DocumentId = Guid.NewGuid().ToString(),
            DocumentType = DetermineDocumentType(fields),
            AnalyzedAt = DateTime.UtcNow,
            OverallConfidence = fields.Count > 0 ? fields.Average(f => f.Confidence) : 0,
            Fields = fields
        };
    }

    private static string DetermineDocumentType(List<ExtractedField> fields)
    {
        var fieldNames = fields.Select(f => f.Name.ToLowerInvariant()).ToHashSet();
        if (fieldNames.Contains("documentnumber") || fieldNames.Contains("passportnumber"))
            return "Identity Document";
        if (fieldNames.Contains("accountnumber") || fieldNames.Contains("statementdate"))
            return "Bank Statement";
        if (fieldNames.Contains("meterreading") || fieldNames.Contains("billingperiod"))
            return "Utility Bill";
        return "Unknown Document";
    }

    private static string DetermineCategory(string fieldName) => fieldName.ToLowerInvariant() switch
    {
        "firstname" or "lastname" or "fullname" or "dateofbirth" => "Personal Information",
        "address" or "city" or "state" or "zipcode" or "country" => "Address",
        "documentnumber" or "passportnumber" or "licensenumber" => "Document ID",
        "expirationdate" or "issuedate" => "Dates",
        _ => "Other"
    };
}
