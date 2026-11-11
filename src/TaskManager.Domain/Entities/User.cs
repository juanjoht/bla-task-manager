namespace TaskManager.Domain.Entities;

public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int NameMaxLength = 100;

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string email, string name, string passwordHash, DateTime utcNow) =>
        throw new NotImplementedException();
}
