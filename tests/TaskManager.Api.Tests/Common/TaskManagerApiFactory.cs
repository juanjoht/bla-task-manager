using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace TaskManager.Api.Tests.Common;

/// <summary>
/// Runs the real API in memory. Each factory gets its own in-memory SQLite database,
/// kept alive by one open connection for the factory's lifetime.
/// </summary>
public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid()};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public TaskManagerApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _connectionString,
            ["Jwt:Issuer"] = "TaskManager",
            ["Jwt:Audience"] = "TaskManager.Client",
            ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-characters!",
            ["Jwt:ExpirationMinutes"] = "60",
            ["App:TimeZone"] = "America/Bogota"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
