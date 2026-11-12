using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests.Common;

/// <summary>
/// A real SQLite database that lives in memory for the duration of one test.
/// The connection stays open because an in-memory SQLite database disappears when its connection closes.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>A fresh context each time, so reads really come from the database and not from EF's cache.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public object? ExecuteScalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    public void Dispose() => _connection.Dispose();
}
