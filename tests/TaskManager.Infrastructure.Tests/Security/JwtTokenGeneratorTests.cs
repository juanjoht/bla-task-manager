using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Tests.Common;
using TaskManager.Infrastructure.Time;

namespace TaskManager.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "TaskManager",
        Audience = "TaskManager.Client",
        SigningKey = "test-signing-key-with-at-least-32-characters!",
        ExpirationMinutes = 60
    };

    private static readonly User DemoUser = new("demo@ballastlane.test", "Demo User", "hashed-password", UtcNow);

    private readonly JwtTokenGenerator _generator =
        new(Options, new SystemClock(new FixedTimeProvider(new DateTimeOffset(UtcNow)), TimeZoneInfo.Utc));

    private static JsonWebToken Read(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);

    [Fact]
    public void GenerateToken_ContainsTheUserIdEmailAndName()
    {
        var token = Read(_generator.GenerateToken(DemoUser));

        token.Subject.Should().Be(DemoUser.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.Should().Be("demo@ballastlane.test");
        token.GetClaim(JwtRegisteredClaimNames.Name).Value.Should().Be("Demo User");
    }

    [Fact]
    public void GenerateToken_NeverContainsThePasswordHash()
    {
        // The payload is only Base64-encoded, so anyone holding the token can read it.
        var token = Read(_generator.GenerateToken(DemoUser));

        token.Claims.Should().NotContain(claim => claim.Value.Contains("hashed-password"));
    }

    [Fact]
    public void GenerateToken_HasTheConfiguredIssuerAndAudience()
    {
        var token = Read(_generator.GenerateToken(DemoUser));

        token.Issuer.Should().Be("TaskManager");
        token.Audiences.Should().ContainSingle().Which.Should().Be("TaskManager.Client");
    }

    [Fact]
    public void GenerateToken_ExpiresAfterTheConfiguredMinutes()
    {
        var token = Read(_generator.GenerateToken(DemoUser));

        token.IssuedAt.Should().Be(UtcNow);
        token.ValidTo.Should().Be(UtcNow.AddMinutes(60));
    }

    [Fact]
    public async Task GenerateToken_IsSignedWithTheConfiguredKey()
    {
        var token = _generator.GenerateToken(DemoUser);

        var withRightKey = await ValidateSignature(token, Options.SigningKey);
        var withWrongKey = await ValidateSignature(token, "another-key-that-is-also-32-characters-long");

        withRightKey.IsValid.Should().BeTrue();
        withWrongKey.IsValid.Should().BeFalse();
    }

    private static Task<TokenValidationResult> ValidateSignature(string token, string key) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false // the token was issued at a fixed past instant
        });
}
