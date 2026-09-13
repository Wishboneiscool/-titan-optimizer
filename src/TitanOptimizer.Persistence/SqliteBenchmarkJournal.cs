using Microsoft.Data.Sqlite;
using TitanOptimizer.Core.Benchmarking;
using TitanOptimizer.Core.Journal;

namespace TitanOptimizer.Persistence;

public sealed class SqliteBenchmarkJournal : IBenchmarkJournal, IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public SqliteBenchmarkJournal(string databasePath)
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

    public void Append(BenchmarkRecord record)
    {
        ThrowIfDisposed();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO benchmark_records
                (session_id, name, captured_utc, duration_ms, work_units, work_units_per_second, related_optimization_id)
            VALUES
                ($session_id, $name, $captured_utc, $duration_ms, $work_units, $work_units_per_second, $related_optimization_id);
            """;
        command.Parameters.AddWithValue("$session_id", record.SessionId.ToString("D"));
        command.Parameters.AddWithValue("$name", record.Name);
        command.Parameters.AddWithValue("$captured_utc", record.CapturedUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$duration_ms", record.Duration.TotalMilliseconds);
        command.Parameters.AddWithValue("$work_units", record.WorkUnits);
        command.Parameters.AddWithValue("$work_units_per_second", record.WorkUnitsPerSecond);
        command.Parameters.AddWithValue("$related_optimization_id", (object?)record.RelatedOptimizationId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<BenchmarkRecord> GetRecent(int limit = 50)
    {
        ThrowIfDisposed();
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT session_id, name, captured_utc, duration_ms, work_units,
                   work_units_per_second, related_optimization_id
            FROM benchmark_records
            ORDER BY captured_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var records = new List<BenchmarkRecord>();
        while (reader.Read())
        {
            records.Add(new BenchmarkRecord
            {
                SessionId = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                CapturedUtc = DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Duration = TimeSpan.FromMilliseconds(reader.GetDouble(3)),
                WorkUnits = reader.GetInt64(4),
                WorkUnitsPerSecond = reader.GetDouble(5),
                RelatedOptimizationId = reader.IsDBNull(6) ? null : reader.GetString(6)
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
            CREATE TABLE IF NOT EXISTS benchmark_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                name TEXT NOT NULL,
                captured_utc TEXT NOT NULL,
                duration_ms REAL NOT NULL,
                work_units INTEGER NOT NULL,
                work_units_per_second REAL NOT NULL,
                related_optimization_id TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_benchmark_records_captured
                ON benchmark_records(captured_utc DESC);
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
