using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DbUp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rake.Core.Helpers;

namespace Rake.Services;

public class DatabaseMigrator : IHostedLifecycleService
{
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(ILogger<DatabaseMigrator> logger) => _logger = logger;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var connectionString = $"Data Source={PathHelper.DataDirectory.Combine("data.db")}";
        var upgrader = DeployChanges
            .To.SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(_logger)
            .Build();

        var required = upgrader.IsUpgradeRequired();

        if (!required)
            return Task.CompletedTask;

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError(result.Error, "Database migration failed.");
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
