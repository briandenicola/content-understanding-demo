namespace ContentUnderstanding.Api.Models;

public record DocumentAnalysisResult
{
    public string DocumentId { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
    public double OverallConfidence { get; init; }
    public List<ExtractedField> Fields { get; init; } = [];
    public string? AgentSummary { get; init; }
}

public record ExtractedField
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string? Category { get; init; }
}
