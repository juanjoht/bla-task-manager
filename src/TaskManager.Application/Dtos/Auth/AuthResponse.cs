namespace TaskManager.Application.Dtos.Auth;

public sealed record AuthResponse(string Token, UserDto User);
