using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WildBunch.Application.Games.Exceptions;

namespace WildBunch.Api.Exceptions;

/// <summary>
/// Maps domain/application exceptions to HTTP status codes so endpoint
/// handlers do not need to repeat try/catch boilerplate. Currently maps:
/// - <see cref="SetupPhaseException"/> → 409 Conflict (gameplay command
///   invoked before the setup flow completed; the session state is fine,
///   the request conflicts with the current lifecycle phase).
/// </summary>
public sealed class WildBunchExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            SetupPhaseException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Setup phase not complete",
                Detail = ex.Message,
                Type = "https://wildbunch.dev/errors/setup-phase"
            },
            _ => null
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }
}
