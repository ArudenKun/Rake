using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record Subtitles([property: JsonPropertyName("rechat")] IReadOnlyList<Rechat> ReChats);
