using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Api.ErrorHandling;

/// <summary>Translates known application and domain exceptions into ProblemDetails responses.</summary>
public sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? statusCode = exception switch
        {
            DomainException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            _ => null
        };

        // Unknown exceptions fall through to the default handler: a generic 500 that does not leak internal details.
        if (statusCode is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode.Value),
                Detail = exception.Message
            }
        });
    }
}
