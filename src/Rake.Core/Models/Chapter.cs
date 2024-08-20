using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Chapter(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("start_time")] int StartTime,
    [property: JsonPropertyName("end_time")] int EndTime
);
