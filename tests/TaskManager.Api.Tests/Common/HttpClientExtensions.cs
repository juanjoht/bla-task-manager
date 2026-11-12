using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests.Common;

internal static class HttpClientExtensions
{
    /// <summary>Logs in and sets the bearer token on the client for the following requests.</summary>
    public static async Task<HttpClient> AuthenticateAsync(
        this HttpClient client, string email = DbSeeder.DemoEmail, string password = DbSeeder.DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}
