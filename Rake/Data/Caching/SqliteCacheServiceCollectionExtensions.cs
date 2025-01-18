// using System;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Rake.Data.Caching;
//
// public static class SqliteCacheServiceCollectionExtensions
// {
//     /// <summary>
//     /// Registers <c>SqliteCache</c> as a dependency-injected singleton, available
//     /// both as <c>IDistributedCache</c> and <c>SqliteCache</c>.
//     /// <br /><br/>
//     /// If you're using ASP.NET Core, install <c>NeoSmart.Caching.Sqlite.AspNetCore</c>
//     /// and add <c>using namespace NeoSmart.Caching.Sqlite.AspNetCore</c> to get a version
//     /// of this method that does not require the <c>sqlite3Provider</c> parameter.
//     /// </summary>
//     /// <param name="services"></param>
//     /// <param name="setupOptions"></param>
//     public static IServiceCollection AddSqliteCache(
//         this IServiceCollection services,
//         Action<SqliteCacheOptions>? setupOptions = null
//     )
//     {
//         services.AddSingleton<SqliteCache>();
//         services.AddSingleton<IDistributedCache, SqliteCache>(serviceProvider =>
//             serviceProvider.GetRequiredService<SqliteCache>()
//         );
//         if (setupOptions is not null)
//         {
//             services.Configure(setupOptions);
//         }
//         return services;
//     }
// }
