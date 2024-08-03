using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record Version(
    [property: JsonPropertyName("version")] string VersionString,
    [property: JsonPropertyName("current_git_head")] object? CurrentGitHead,
    [property: JsonPropertyName("release_git_head")] string ReleaseGitHead,
    [property: JsonPropertyName("repository")] string Repository
);
