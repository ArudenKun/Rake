using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Rake.Configuration.Writable;

namespace Rake.Settings;

[Options]
public sealed partial class Setting : ObservableObject
{
    [ObservableProperty]
    public partial bool IsFirstRun { get; set; }

    [ObservableProperty]
    public partial LoggingSetting Logging { get; set; } = new();

    [JsonSerializable(typeof(Setting))]
    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    public sealed partial class SerializerContext : JsonSerializerContext;
}
