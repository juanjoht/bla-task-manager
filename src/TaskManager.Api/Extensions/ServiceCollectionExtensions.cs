using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Dtos.Auth;
using TaskManager.Application.Dtos.Tasks;
using TaskManager.Application.Services;
using TaskManager.Application.Validators;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Time;

namespace TaskManager.Api.Extensions;

/// <summary>Composition root: the only place where interfaces are bound to their implementations.</summary>
public static class ServiceCollectionExtensions
{
    private const int MinimumSigningKeyLength = 32;

    // Settings are read lazily (when a service is first built), never while registering,
    // so values added later in the startup pipeline (e.g. by integration tests) are honoured.
    public static IServiceCollection AddTaskManager(this IServiceCollection services)
    {
        // Infrastructure
        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseSqlite(ReadConnectionString(provider.GetRequiredService<IConfiguration>())));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<DbSeeder>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options => options.SigningKey.Length >= MinimumSigningKeyLength,
                $"'Jwt:SigningKey' must be at least {MinimumSigningKeyLength} characters.")
            .ValidateOnStart(); // fail fast at startup with a clear message
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<JwtOptions>>().Value);
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock>(provider => new SystemClock(
            provider.GetRequiredService<TimeProvider>(),
            ReadTimeZone(provider.GetRequiredService<IConfiguration>())));

        // Application
        services.AddScoped<AuthService>();
        services.AddScoped<TaskService>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
        services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();

        return services;
    }

    private static string ReadConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'.");

    private static TimeZoneInfo ReadTimeZone(IConfiguration configuration) =>
        TimeZoneInfo.FindSystemTimeZoneById(configuration["App:TimeZone"]
            ?? throw new InvalidOperationException("Missing 'App:TimeZone' setting."));
}
