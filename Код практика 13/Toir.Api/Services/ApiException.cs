using Toir.Api.Models.Responses;

namespace Toir.Api.Services;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public List<FieldError> Details { get; }

    public ApiException(int statusCode, string errorCode, string message, List<FieldError>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Details = details ?? [];
    }
}
