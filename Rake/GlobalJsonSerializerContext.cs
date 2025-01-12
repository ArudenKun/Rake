using System.Text.Json.Serialization;
using Rake.Services;

namespace Rake;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(SettingsService))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper,
    UseStringEnumConverter = true,
    WriteIndented = true
)]
public sealed partial class GlobalJsonSerializerContext : JsonSerializerContext;
