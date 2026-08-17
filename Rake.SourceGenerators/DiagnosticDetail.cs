namespace Rake.SourceGenerators;

public record DiagnosticDetail
{
    public string? Id { get; init; } = string.Empty;
    public string? Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
