using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Rake.Helpers;

namespace Rake.Data;

public sealed class SqliteDatabase : IDisposable, IAsyncDisposable
{
    private readonly ILogger<SqliteDatabase> _logger;

    private static readonly string ConnectionString = PathHelper.DatabasePath;
    private const int InitialConcurrency = 4;
    private readonly ConcurrentBag<SqliteConnection> _connections = [];

    public SqliteDatabase(ILogger<SqliteDatabase> logger)
    {
        _logger = logger;

        _logger.LogInformation(
            "Creating {InitialConnections} initial connections in the pool",
            InitialConcurrency
        );
        for (var i = 0; i < InitialConcurrency; ++i)
        {
            _connections.Add(CreateConnection());
        }
    }

    public void Use(Action<SqliteConnection, SqliteTransaction> handler) =>
        Use(
            (db, tran) =>
            {
                handler(db, tran);
                return true;
            }
        );

    public void Use(Action<SqliteConnection> handler) =>
        Use(db =>
        {
            handler(db);
            return true;
        });

    public TResult Use<TResult>(Func<SqliteConnection, SqliteTransaction, TResult> handler) =>
        Use(db =>
        {
            using var transaction = db.BeginTransaction();
            return handler(db, transaction);
        });

    public TResult Use<TResult>(Func<SqliteConnection, TResult> handler)
    {
        if (!_connections.TryTake(out var db))
        {
            _logger.LogInformation("Adding a new connection to the connection pool");
            db = CreateConnection();
        }

        try
        {
            return handler(db);
        }
        finally
        {
            _connections.Add(db);
        }
    }

    public Task UseAsync(Func<SqliteConnection, DbTransaction, Task> handler) =>
        UseAsync(
            async (db, tran) =>
            {
                await handler(db, tran);
                return true;
            }
        );

    public Task UseAsync(Func<SqliteConnection, Task> handler) =>
        UseAsync(async db =>
        {
            await handler(db);
            return true;
        });

    public Task<TResult> UseAsync<TResult>(
        Func<SqliteConnection, DbTransaction, Task<TResult>> handler,
        CancellationToken cancellationToken = default
    ) =>
        UseAsync(async db =>
        {
            await using var transaction = await db.BeginTransactionAsync(cancellationToken);
            return await handler(db, transaction);
        });

    public async Task<TResult> UseAsync<TResult>(Func<SqliteConnection, Task<TResult>> handler)
    {
        if (!_connections.TryTake(out var db))
        {
            _logger.LogInformation("Adding a new connection to the connection pool");
            db = await CreateConnectionAsync();
        }

        try
        {
            return await handler(db);
        }
        finally
        {
            _connections.Add(db);
        }
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        LogOpeningConnection();
        connection.Open();
        return connection;
    }

    private async Task<SqliteConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var connection = new SqliteConnection(ConnectionString);
        LogOpeningConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public void Dispose()
    {
        foreach (var conn in _connections)
        {
            LogClosingConnection();
            conn.Close();
            conn.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _connections)
        {
            LogClosingConnection();
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }
    }

    private void LogOpeningConnection() =>
        _logger.LogInformation("Opening connection to {SqliteCacheDbPath}", ConnectionString);

    private void LogClosingConnection() =>
        _logger.LogInformation("Closing connection to {SqliteCacheDbPath}", ConnectionString);
}
