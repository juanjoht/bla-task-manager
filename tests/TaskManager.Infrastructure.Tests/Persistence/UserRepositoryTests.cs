using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Infrastructure.Tests.Common;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<User> GivenSavedUser(string email = "demo@ballastlane.test")
    {
        var user = new User(email, "Demo User", "hashed-password", UtcNow);
        await using var context = _database.CreateContext();
        await new UserRepository(context).AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Add_PersistsTheUser()
    {
        var user = await GivenSavedUser();

        await using var context = _database.CreateContext();
        context.Users.Should().ContainSingle(u => u.Id == user.Id);
    }

    [Fact]
    public async Task GetById_ReturnsTheUserOrNull()
    {
        var user = await GivenSavedUser();
        await using var context = _database.CreateContext();
        var repository = new UserRepository(context);

        (await repository.GetByIdAsync(user.Id, CancellationToken.None))!.Email.Should().Be("demo@ballastlane.test");
        (await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_ReturnsTheUserOrNull()
    {
        var user = await GivenSavedUser();
        await using var context = _database.CreateContext();
        var repository = new UserRepository(context);

        (await repository.GetByEmailAsync("demo@ballastlane.test", CancellationToken.None))!.Id.Should().Be(user.Id);
        (await repository.GetByEmailAsync("nobody@ballastlane.test", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task EmailExists_ReflectsWhatIsStored()
    {
        await GivenSavedUser();
        await using var context = _database.CreateContext();
        var repository = new UserRepository(context);

        (await repository.EmailExistsAsync("demo@ballastlane.test", CancellationToken.None)).Should().BeTrue();
        (await repository.EmailExistsAsync("nobody@ballastlane.test", CancellationToken.None)).Should().BeFalse();
    }
}
