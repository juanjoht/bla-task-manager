using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.Tests.Common;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests.Controllers;

public sealed class HealthControllerTests : IDisposable
{
    private readonly TaskManagerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_IsPublic_AndReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Startup_AppliesMigrationsAndSeedsTheDemoUser()
    {
        _factory.CreateClient(); // starts the app

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Should().ContainSingle(u => u.Email == DbSeeder.DemoEmail);
        db.Tasks.Should().HaveCount(5);
    }
}
