namespace Toir.Api.Models.Responses;

public sealed record ErrorResponse(ErrorInfo Error, string TraceId);

public sealed record ErrorInfo(string Code, string Message, List<FieldError> Details);

public sealed record FieldError(string Field, string Message);
