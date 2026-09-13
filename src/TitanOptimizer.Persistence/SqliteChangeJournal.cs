using Microsoft.Data.Sqlite;
using TitanOptimizer.Core.Journal;
using TitanOptimizer.Core.Models;

namespace TitanOptimizer.Persistence;

public sealed class SqliteChangeJournal : IChangeJournal, IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public SqliteChangeJournal(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The database path must include a directory.", nameof(databasePath));

        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        Initialize();
    }

    public void Append(ChangeRecord record)
    {
        ThrowIfDisposed();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO change_records
                (session_id, optimization_id, timestamp_utc, before_value, requested_value, after_value, result, rollback_available, error)
            VALUES
                ($session_id, $optimization_id, $timestamp_utc, $before_value, $requested_value, $after_value, $result, $rollback_available, $error);
            """;
        command.Parameters.AddWithValue("$session_id", record.SessionId.ToString("D"));
        command.Parameters.AddWithValue("$optimization_id", record.OptimizationId);
        command.Parameters.AddWithValue("$timestamp_utc", record.TimestampUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$before_value", record.Before);
        command.Parameters.AddWithValue("$requested_value", record.Requested);
        command.Parameters.AddWithValue("$after_value", (object?)record.After ?? DBNull.Value);
        command.Parameters.AddWithValue("$result", record.Result);
        command.Parameters.AddWithValue("$rollback_available", record.RollbackAvailable ? 1 : 0);
        command.Parameters.AddWithValue("$error", (object?)record.Error ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ChangeRecord> GetRecent(int limit = 50)
    {
        ThrowIfDisposed();
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "The journal limit must be between 1 and 500.");
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT session_id, optimization_id, timestamp_utc, before_value, requested_value,
                   after_value, result, rollback_available, error
            FROM change_records
            ORDER BY timestamp_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var records = new List<ChangeRecord>();
        while (reader.Read())
        {
            records.Add(new ChangeRecord
            {
                SessionId = Guid.Parse(reader.GetString(0)),
                OptimizationId = reader.GetString(1),
                TimestampUtc = DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Before = reader.GetString(3),
                Requested = reader.GetString(4),
                After = reader.IsDBNull(5) ? null : reader.GetString(5),
                Result = reader.GetString(6),
                RollbackAvailable = reader.GetInt32(7) != 0,
                Error = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return records;
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS change_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                optimization_id TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL,
                before_value TEXT NOT NULL,
                requested_value TEXT NOT NULL,
                after_value TEXT NULL,
                result TEXT NOT NULL,
                rollback_available INTEGER NOT NULL,
                error TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_change_records_timestamp
                ON change_records(timestamp_utc DESC);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
