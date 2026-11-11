using System.Text.RegularExpressions;
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

    // Required by EF Core to materialize entities from the database.
    private User()
    {
    }

    public static User Create(string email, string name, string passwordHash, DateTime utcNow)
    {
        var validEmail = ValidateEmail(email);
        var validName = ValidateName(name);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = validEmail,
            Name = validName,
            PasswordHash = passwordHash,
            CreatedAt = utcNow
        };
    }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > EmailMaxLength)
        {
            throw new DomainException($"Email cannot exceed {EmailMaxLength} characters.");
        }

        if (!EmailFormat().IsMatch(normalized))
        {
            throw new DomainException("Email format is not valid.");
        }

        return normalized;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"Name cannot exceed {NameMaxLength} characters.");
        }

        return trimmed;
    }

    // Pragmatic check (something@domain.tld, no spaces, single @), not full RFC 5322.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormat();
}
