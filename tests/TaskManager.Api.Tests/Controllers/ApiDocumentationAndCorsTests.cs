using System.Net;
using FluentAssertions;
using TaskManager.Api.Tests.Common;

namespace TaskManager.Api.Tests.Controllers;

public sealed class ApiDocumentationAndCorsTests : IDisposable
{
    private const string AngularOrigin = "http://localhost:4200";

    private readonly TaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiDocumentationAndCorsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OpenApiDocument_DescribesTheEndpointsAndTheBearerScheme()
    {
        var json = await _client.GetStringAsync("/openapi/v1.json");

        json.Should().Contain("/api/tasks").And.Contain("/api/auth/login");
        json.Should().Contain("\"bearer\"");
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cors_AllowsTheAngularApp()
    {
        var response = await _client.SendAsync(Preflight(AngularOrigin));

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(AngularOrigin);
    }

    [Fact]
    public async Task Cors_RejectsUnknownOrigins()
    {
        var response = await _client.SendAsync(Preflight("http://evil.example"));

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    // The browser sends this "preflight" before a cross-origin request with an Authorization header.
    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        return request;
    }
}
