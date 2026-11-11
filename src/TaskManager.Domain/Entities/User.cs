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
        Email = ValidateEmail(email);
        Name = Guard.RequiredMaxLength(name, NameMaxLength, "Name");
        PasswordHash = Guard.Required(passwordHash, "Password hash");
        CreatedAt = utcNow;
    }

    // Used only by EF Core to load existing rows.
    private User()
    {
    }

    // Single source of truth: "Demo@X.com" and "demo@x.com" are the same account (used by register and login).
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    // Single source of truth for the email format (used by the entity and by input validation).
    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailFormat().IsMatch(NormalizeEmail(email));

    private static string ValidateEmail(string email)
    {
        var normalized = NormalizeEmail(Guard.RequiredMaxLength(email, EmailMaxLength, "Email"));

        if (!IsValidEmail(normalized))
        {
            throw new DomainException("Email format is not valid.");
        }

        return normalized;
    }

    // Pragmatic check (something@domain.tld, no spaces, single @), not full RFC 5322.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormat();
}
