// using System;
//
// namespace Rake.Data.Caching;
//
// public record SqliteCacheOptions(TimeSpan? CleanupInterval = null)
// {
//     /// <summary>
//     /// Specifies how often expired items are removed in the background.
//     /// Background eviction is disabled if set to <c>null</c>.
//     /// </summary>
//     public TimeSpan? CleanupInterval { get; } = CleanupInterval ?? TimeSpan.FromMinutes(30);
// }
