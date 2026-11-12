using FluentAssertions;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Tests.Common;
using TaskManager.Infrastructure.Time;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class DbSeederTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    private readonly SqliteTestDatabase _database = new();
    private readonly PasswordHasher _passwordHasher = new();

    public void Dispose() => _database.Dispose();

    private async Task SeedAsync()
    {
        await using var context = _database.CreateContext();
        var clock = new SystemClock(new FixedTimeProvider(new DateTimeOffset(UtcNow)), TimeZoneInfo.Utc);
        await new DbSeeder(context, _passwordHasher, clock).SeedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Seed_CreatesTheDemoUserWithAWorkingPassword()
    {
        await SeedAsync();

        await using var context = _database.CreateContext();
        var demo = context.Users.Single();
        demo.Email.Should().Be(DbSeeder.DemoEmail);
        demo.Name.Should().Be("Demo User");
        _passwordHasher.Verify(DbSeeder.DemoPassword, demo.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Seed_CreatesFiveTasksForTheDemoUserInEveryStatus()
    {
        await SeedAsync();

        await using var context = _database.CreateContext();
        var demoId = context.Users.Single().Id;
        var tasks = context.Tasks.ToList();

        tasks.Should().HaveCount(5).And.OnlyContain(t => t.UserId == demoId);
        tasks.Select(t => t.Status).Should().Contain([TaskItemStatus.Pending, TaskItemStatus.InProgress, TaskItemStatus.Done]);
    }

    [Fact]
    public async Task Seed_UsesDueDatesRelativeToToday()
    {
        // Relative dates keep the demo data current instead of slowly becoming overdue.
        await SeedAsync();

        await using var context = _database.CreateContext();
        context.Tasks.Select(t => t.DueDate).ToList().Should().OnlyContain(date => date >= Today && date <= Today.AddDays(7));
    }

    [Fact]
    public async Task Seed_RunTwice_DoesNotDuplicateData()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = _database.CreateContext();
        context.Users.Should().HaveCount(1);
        context.Tasks.Should().HaveCount(5);
    }
}
