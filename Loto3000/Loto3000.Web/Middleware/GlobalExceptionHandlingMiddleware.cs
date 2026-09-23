using Loto3000.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Loto3000.Web.Middleware;

/// <summary>Translates failures into safe problem JSON while retaining diagnostic causes in server logs.</summary>
/// <param name="next">The remaining HTTP pipeline.</param>
/// <param name="logger">Structured exception logging correlated by trace identifier.</param>
public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    /// <summary>Executes the pipeline and maps business, identity and unexpected failures to 400, 401 and 500.</summary>
    /// <param name="context">The current request and response.</param>
    /// <returns>A task completing after the response is produced or a started-response error is rethrown.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {TraceId} was canceled by the client.", context.TraceIdentifier);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (BusinessRuleException exception)
        {
            // Only explicitly safe business messages are returned; SQL/stack details stay in the log.
            if (context.Response.HasStarted) throw;
            logger.LogWarning(exception, "Business rule rejected request {TraceId}.", context.TraceIdentifier);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Request rejected", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            if (context.Response.HasStarted) throw;
            logger.LogWarning(exception, "Invalid authenticated identity for request {TraceId}.", context.TraceIdentifier);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Authentication required", "Please sign in again.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error on request {TraceId}.", context.TraceIdentifier);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Unexpected error",
                "An unexpected error occurred. Try again or contact support with the trace ID.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        // Replace any partial payload, not the trace ID; OnStarting reapplies the security headers.
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
