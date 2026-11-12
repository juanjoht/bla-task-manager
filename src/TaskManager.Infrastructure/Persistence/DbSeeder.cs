using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Creates the demo user and sample tasks through the domain, so seed data obeys the same rules as real data.</summary>
public sealed class DbSeeder(AppDbContext db, IPasswordHasher passwordHasher, IClock clock)
{
    public const string DemoEmail = "demo@ballastlane.test";
    public const string DemoPassword = "Demo123!";

    public Task SeedAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
