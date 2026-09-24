using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuickFlow.Domain.Common;

namespace QuickFlow.Api.Infrastructure;

/// <summary>Maps domain exceptions to RFC 7807 ProblemDetails responses.</summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public DomainExceptionHandler(IProblemDetailsService problemDetails) => _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            DomainValidationException v => new ValidationProblemDetails(v.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            },
            NotFoundException nf => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = nf.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            },
            ConflictException c => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = c.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            },
            _ => null,
        };

        if (problem is null) return false;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
