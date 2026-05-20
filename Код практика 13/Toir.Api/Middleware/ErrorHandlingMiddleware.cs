using Toir.Api.Models.Responses;
using Toir.Api.Services;

namespace Toir.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            LogKnownError(ex, context);
            await WriteError(context, ex.StatusCode, ex.ErrorCode, ex.Message, ex.Details);
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogInformation(ex, "Bad request TraceId={TraceId}", context.Items["TraceId"]);
            await WriteError(context, StatusCodes.Status400BadRequest, "BAD_REQUEST", "Некорректный HTTP-запрос.", []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error TraceId={TraceId}", context.Items["TraceId"]);
            await WriteError(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Внутренняя ошибка сервера.", []);
        }
    }

    private void LogKnownError(ApiException ex, HttpContext context)
    {
        var traceId = context.Items["TraceId"];

        if (ex.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            logger.LogWarning(ex, "Access error Code={Code} TraceId={TraceId}", ex.ErrorCode, traceId);
            return;
        }

        if (ex.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity)
        {
            logger.LogInformation(ex, "Validation error Code={Code} TraceId={TraceId}", ex.ErrorCode, traceId);
            return;
        }

        logger.LogInformation(ex, "API error Code={Code} TraceId={TraceId}", ex.ErrorCode, traceId);
    }

    private static async Task WriteError(HttpContext context, int statusCode, string code, string message, List<FieldError> details)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var traceId = context.Items.TryGetValue("TraceId", out var value) && value is string id
            ? id
            : context.TraceIdentifier;

        var response = new ErrorResponse(new ErrorInfo(code, message, details), traceId);
        await context.Response.WriteAsJsonAsync(response);
    }
}
