namespace TaskManager.Application.Common.Exceptions;

/// <summary>The request clashes with existing data (mapped to HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);
