using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.Concurrency;

public sealed class PostgresAdvisoryLockManager
{
    private readonly string _connectionString;

    public PostgresAdvisoryLockManager(IConfiguration configuration)
    {
        _connectionString = configuration["CONNECTIONSTRING"]
            ?? configuration.GetConnectionString("IceBot_DB")
            ?? throw new InvalidOperationException("Database connection string is required for distributed job locks.");
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(long lockKey, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@lock_key);", connection);
        command.Parameters.AddWithValue("lock_key", lockKey);
        var acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        if (!acquired)
        {
            await connection.DisposeAsync();
            return null;
        }

        return new AdvisoryLockHandle(connection, lockKey);
    }

    private sealed class AdvisoryLockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _lockKey;
        private bool _disposed;

        public AdvisoryLockHandle(NpgsqlConnection connection, long lockKey)
        {
            _connection = connection;
            _lockKey = lockKey;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@lock_key);", _connection);
                command.Parameters.AddWithValue("lock_key", _lockKey);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
