namespace Toir.Api.Middleware;

public sealed class TraceLoggingMiddleware(RequestDelegate next, ILogger<TraceLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var traceId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = Guid.NewGuid().ToString("N");
        }

        context.Items["TraceId"] = traceId;
        context.Response.Headers["X-Correlation-Id"] = traceId;

        await next(context);

        var role = context.Request.Headers["X-User-Role"].FirstOrDefault() ?? "anonymous";
        logger.LogInformation(
            "Endpoint={Endpoint} Method={Method} Role={Role} StatusCode={StatusCode} TraceId={TraceId}",
            context.Request.Path,
            context.Request.Method,
            role,
            context.Response.StatusCode,
            traceId);
    }
}
