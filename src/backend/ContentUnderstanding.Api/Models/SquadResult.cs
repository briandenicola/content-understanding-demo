namespace ContentUnderstanding.Api.Models;

public record SquadResult
{
    public List<string> AgentOutputs { get; init; } = [];
    public DateTime ProcessedAt { get; init; }
}
