using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class MigrationTests : IDisposable
{
    // Not using SqliteTestDatabase here: it calls EnsureCreated, which would bypass the migrations under test.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public MigrationTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public void Model_HasNoChangesMissingFromTheMigrations()
    {
        // Fails if an entity or configuration changes and nobody runs "dotnet ef migrations add".
        using var context = CreateContext();

        context.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void Migrations_CreateTheUsersAndTasksTables()
    {
        using var context = CreateContext();

        context.Database.Migrate();

        context.Users.Should().BeEmpty();
        context.Tasks.Should().BeEmpty();
    }
}
