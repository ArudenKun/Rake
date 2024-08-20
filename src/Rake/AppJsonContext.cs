using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Rake.Core.Models;
using Rake.Models;
using Rake.Services;
using Rake.Services.Caching;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;

namespace Rake;

[JsonSerializable(typeof(Video))]
[JsonSerializable(typeof(Playlist))]
[JsonSerializable(typeof(SettingsService))]
[JsonSerializable(typeof(ConcurrentDictionary<string, ManifestEntry>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<byte[]>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<bool>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<string>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<int>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<Theme>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<Dictionary<string, string>>))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class AppJsonContext : JsonSerializerContext;
