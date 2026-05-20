namespace Toir.Api.Models.Responses;

public sealed record ApiResponse<T>(T Data, string TraceId, DateTime Timestamp);
