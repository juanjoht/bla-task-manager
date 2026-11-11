namespace TaskManager.Application.Auth;

public sealed record AuthResponse(string Token, UserDto User);
