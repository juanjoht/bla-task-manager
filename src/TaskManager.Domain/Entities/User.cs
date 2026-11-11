using System.Text.RegularExpressions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed partial class User
{
    public const int EmailMaxLength = 254;
    public const int NameMaxLength = 100;

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public User(string email, string name, string passwordHash, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        Name = Guard.RequiredMaxLength(name, NameMaxLength, "Name");
        PasswordHash = Guard.Required(passwordHash, "Password hash");
        CreatedAt = utcNow;
    }

    // Used only by EF Core to load existing rows.
    private User()
    {
    }

    // Lower-cased so "Demo@X.com" and "demo@x.com" are the same account for the uniqueness check.
    private static string NormalizeEmail(string email)
    {
        var normalized = Guard.RequiredMaxLength(email, EmailMaxLength, "Email").ToLowerInvariant();

        if (!EmailFormat().IsMatch(normalized))
        {
            throw new DomainException("Email format is not valid.");
        }

        return normalized;
    }

    // Pragmatic check (something@domain.tld, no spaces, single @), not full RFC 5322.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormat();
}
