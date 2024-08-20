using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Subtitles([property: JsonPropertyName("rechat")] IReadOnlyList<Rechat> ReChats);
