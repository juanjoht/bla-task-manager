namespace TaskManager.Application.Auth;

public sealed record RegisterRequest(string Email, string Name, string Password);
