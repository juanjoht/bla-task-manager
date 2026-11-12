using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Security;

public sealed class JwtTokenGenerator(JwtOptions options, IClock clock) : ITokenGenerator
{
    public string GenerateToken(User user)
    {
        var now = clock.UtcNow;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(options.ExpirationMinutes),
            // Only non-sensitive identity data: the payload is readable by anyone holding the token.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.Name
            },
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
