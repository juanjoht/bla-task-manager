namespace TaskManager.Application.Common.Exceptions;

/// <summary>The resource does not exist or is not visible to the current user (mapped to HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);
