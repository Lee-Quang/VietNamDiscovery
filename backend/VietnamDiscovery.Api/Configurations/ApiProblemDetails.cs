using System.Diagnostics;

namespace VietnamDiscovery.Api.Configurations;

public static class ApiProblemDetails
{
    public static void Customize(ProblemDetailsContext context)
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] =
            GetTraceId(context.HttpContext);
    }

    // Results.Problem falls back to JSON when Accept isn't supported by the problem
    // writer. Supply common fields here too so the fallback has the same contract.
    public static IResult CreateResult(HttpContext context, int statusCode,
        string? title = null, string? detail = null) => Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = GetTraceId(context) });

    public static string GetTraceId(HttpContext context) =>
        Activity.Current?.Id ?? context.TraceIdentifier;
}
