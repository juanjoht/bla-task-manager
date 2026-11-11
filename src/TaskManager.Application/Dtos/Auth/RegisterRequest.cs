namespace TaskManager.Application.Dtos.Auth;

public sealed record RegisterRequest(string Email, string Name, string Password);
