using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Tests.Common;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests.Controllers;

public sealed class AuthControllerTests : IDisposable
{
    private readonly TaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthControllerTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // ---------- Register ----------

    [Fact]
    public async Task Register_WithValidData_Returns201WithTheUserAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("new.user@ballastlane.test", "New User", "Secret123"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be("/api/auth/me");
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Email.Should().Be("new.user@ballastlane.test");
    }

    [Fact]
    public async Task Register_ThenLogin_Works()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("new.user@ballastlane.test", "New User", "Secret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("new.user@ballastlane.test", "Secret123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400WithEveryFieldError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("not-an-email", "", "short"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(["Email", "Name", "Password"]);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(DbSeeder.DemoEmail, "Someone", "Secret123"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Email is already registered.");
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithTheDemoCredentials_Returns200WithAToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, DbSeeder.DemoPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(DbSeeder.DemoEmail);
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, "Wrong123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Me (authorized endpoint) ----------

    [Fact]
    public async Task Me_WithoutAToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAnInvalidToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAValidToken_ReturnsTheLoggedInUser()
    {
        await _client.AuthenticateAsync();

        var user = await _client.GetFromJsonAsync<UserDto>("/api/auth/me");

        user!.Email.Should().Be(DbSeeder.DemoEmail);
        user.Name.Should().Be("Demo User");
    }
}
