// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using AutoInterfaceAttributes;
// using Dommel;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Logging;
// using R3;
//
// namespace Rake.Data.Caching;
//
// [AutoInterface(Inheritance = [typeof(IDistributedCache), typeof(IDisposable)])]
// public sealed class SqliteCache : ISqliteCache
// {
//     private readonly SqliteDatabase _database;
//     private readonly ILogger<SqliteCache> _logger;
//
//     private readonly IDisposable? _cleanupSubscription;
//
//     public SqliteCache(
//         SqliteCacheOptions options,
//         SqliteDatabase database,
//         ILogger<SqliteCache> logger
//     )
//     {
//         _database = database;
//         _logger = logger;
//
//         if (options.CleanupInterval.HasValue)
//         {
//             _cleanupSubscription = Observable
//                 .Interval(options.CleanupInterval.Value)
//                 .SubscribeAwait(
//                     async (_, token) =>
//                     {
//                         _logger.LogInformation(
//                             "Beginning background cleanup of expired SqliteCache items"
//                         );
//                         await RemoveExpiredAsync(token);
//                     }
//                 );
//         }
//     }
//
//     public byte[]? Get(string key) =>
//         _database.Use(connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             return connection
//                 .FirstOrDefault<SqliteCacheEntry>(p =>
//                     p.Key == key && (p.Expiry == null || p.Expiry >= now.Ticks)
//                 )
//                 ?.Value;
//         });
//
//     public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
//         _database.UseAsync(async connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             var result = await connection.FirstOrDefaultAsync<SqliteCacheEntry>(
//                 p => p.Key == key && (p.Expiry == null || p.Expiry >= now.Ticks),
//                 cancellationToken: token
//             );
//             return result?.Value;
//         });
//
//     public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
//         _database.Use(connection =>
//         {
//             var oldEntry = connection.Get<SqliteCacheEntry>(key);
//             if (oldEntry is not null)
//                 Remove(key);
//
//             var entry = new SqliteCacheEntry(key, value);
//             AddExpiration(ref entry, options);
//             connection.Insert(entry);
//         });
//
//     public Task SetAsync(
//         string key,
//         byte[] value,
//         DistributedCacheEntryOptions options,
//         CancellationToken token = default
//     ) =>
//         _database.UseAsync(async connection =>
//         {
//             var oldEntry = await connection.GetAsync<SqliteCacheEntry>(
//                 key,
//                 cancellationToken: token
//             );
//
//             if (oldEntry is not null)
//                 await RemoveAsync(key, token);
//
//             var entry = new SqliteCacheEntry(key, value);
//             AddExpiration(ref entry, options);
//             await connection.InsertAsync(entry, cancellationToken: token);
//         });
//
//     public void Refresh(string key) =>
//         _database.Use(connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             var entry = connection.FirstOrDefault<SqliteCacheEntry>(p =>
//                 p.Key == key && p.Expiry >= now.Ticks && p.Renewal != null
//             );
//             if (entry is null)
//                 return;
//             entry = entry with { Expiry = now.Ticks + entry.Renewal };
//             connection.Update(entry);
//         });
//
//     public Task RefreshAsync(string key, CancellationToken token = default) =>
//         _database.UseAsync(async connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             var entry = await connection.FirstOrDefaultAsync<SqliteCacheEntry>(
//                 p => p.Key == key && p.Expiry >= now.Ticks && p.Renewal != null,
//                 cancellationToken: token
//             );
//             if (entry is null)
//                 return;
//             entry = entry with { Expiry = now.Ticks + entry.Renewal };
//             await connection.UpdateAsync(entry, cancellationToken: token);
//         });
//
//     public void Remove(string key) =>
//         _database.Use(connection =>
//         {
//             var entry = connection.Get<SqliteCacheEntry>(key);
//             connection.Delete(entry);
//         });
//
//     public Task RemoveAsync(string key, CancellationToken token = default) =>
//         _database.Use(async connection =>
//         {
//             var entry = await connection.GetAsync<SqliteCacheEntry>(key, cancellationToken: token);
//             await connection.DeleteAsync(entry, cancellationToken: token);
//         });
//
//     public void RemoveExpired() =>
//         _database.Use(connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             var removed = connection.DeleteMultiple<SqliteCacheEntry>(p =>
//                 !(p.Expiry == null || p.Expiry >= now.Ticks)
//             );
//
//             if (removed > 0)
//             {
//                 _logger.LogInformation(
//                     "Evicted {DeletedCacheEntryCount} expired entries from cache",
//                     removed
//                 );
//             }
//         });
//
//     public Task RemoveExpiredAsync(CancellationToken token = default) =>
//         _database.UseAsync(async connection =>
//         {
//             var now = DateTimeOffset.UtcNow;
//             var removed = await connection.DeleteMultipleAsync<SqliteCacheEntry>(
//                 p => !(p.Expiry == null || p.Expiry >= now.Ticks),
//                 cancellationToken: token
//             );
//
//             if (removed > 0)
//             {
//                 _logger.LogInformation(
//                     "Evicted {DeletedCacheEntryCount} expired entries from cache",
//                     removed
//                 );
//             }
//         });
//
//     public void Dispose() => _cleanupSubscription?.Dispose();
//
//     private static void AddExpiration(
//         ref SqliteCacheEntry entry,
//         DistributedCacheEntryOptions options
//     )
//     {
//         DateTimeOffset? expiry = null;
//         TimeSpan? renewal = null;
//
//         if (options.AbsoluteExpiration.HasValue)
//         {
//             expiry = options.AbsoluteExpiration.Value.ToUniversalTime();
//         }
//         else if (options.AbsoluteExpirationRelativeToNow.HasValue)
//         {
//             expiry = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
//         }
//
//         if (options.SlidingExpiration.HasValue)
//         {
//             renewal = options.SlidingExpiration.Value;
//             expiry = (expiry ?? DateTimeOffset.UtcNow) + renewal;
//         }
//
//         entry = entry with { Expiry = expiry?.Ticks, Renewal = renewal?.Ticks };
//     }
// }
