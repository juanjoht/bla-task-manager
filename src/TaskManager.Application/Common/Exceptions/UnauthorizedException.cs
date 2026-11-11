namespace TaskManager.Application.Common.Exceptions;

/// <summary>Missing or invalid credentials (mapped to HTTP 401).</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
