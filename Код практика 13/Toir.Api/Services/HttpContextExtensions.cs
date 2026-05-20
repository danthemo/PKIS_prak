namespace Toir.Api.Services;

public static class HttpContextExtensions
{
    public static string GetTraceId(this HttpContext context)
    {
        return context.Items.TryGetValue("TraceId", out var value) && value is string traceId
            ? traceId
            : context.TraceIdentifier;
    }
}
