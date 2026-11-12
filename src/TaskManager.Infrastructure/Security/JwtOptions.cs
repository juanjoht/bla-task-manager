namespace TaskManager.Infrastructure.Security;

/// <summary>JWT settings, bound from the "Jwt" section of appsettings.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 secret; must be at least 32 characters (256 bits).</summary>
    public string SigningKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 60;
}
