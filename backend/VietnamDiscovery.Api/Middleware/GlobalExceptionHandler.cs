using Microsoft.AspNetCore.Diagnostics;
using VietnamDiscovery.Api.Configurations;

namespace VietnamDiscovery.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled request error. TraceId: {TraceId}",
            ApiProblemDetails.GetTraceId(httpContext));

        await ApiProblemDetails.CreateResult(httpContext,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            detail: "An unexpected error occurred. Contact support with the traceId.")
            .ExecuteAsync(httpContext);

        return true;
    }
}
