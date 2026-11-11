using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Common;

/// <summary>Reusable invariant checks. Each one throws <see cref="DomainException"/> with a readable message.</summary>
internal static class Guard
{
    public static void NotEmpty(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{field} is required.");
        }
    }

    public static string Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        return value;
    }

    public static string RequiredMaxLength(string value, int maxLength, string field)
    {
        var trimmed = Required(value, field).Trim();
        return MaxLength(trimmed, maxLength, field);
    }

    public static string? OptionalMaxLength(string? value, int maxLength, string field) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), maxLength, field);

    public static void NotInPast(DateOnly date, DateOnly today, string field)
    {
        if (date < today)
        {
            throw new DomainException($"{field} cannot be in the past.");
        }
    }

    public static void Defined<TEnum>(TEnum value, string field) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{field} is not valid.");
        }
    }

    private static string MaxLength(string value, int maxLength, string field)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");
        }

        return value;
    }
}
