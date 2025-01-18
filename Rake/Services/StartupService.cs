using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rake.Helpers;

namespace Rake.Services;

public class StartupService : IHostedLifecycleService
{
    private readonly ILogger<StartupService> _logger;
    private readonly SettingsService _settingsService;
    private readonly IConfiguration _configuration;

    public StartupService(
        ILogger<StartupService> logger,
        SettingsService settingsService,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _settingsService = settingsService;
        _configuration = configuration;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _settingsService.Load();

        ApplyMigrations();
        return Task.CompletedTask;
    }

    private void ApplyMigrations()
    {
        var upgrader = DeployChanges
            .To.SqliteDatabase(PathHelper.DatabaseConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(_logger)
            .Build();

        var required = upgrader.IsUpgradeRequired();

        if (!required)
            return;

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError(result.Error, "Database migration failed.");
        }
        else
        {
            _logger.LogInformation("Database migration completed");
        }
    }

    #region Unused

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

    #endregion
}
