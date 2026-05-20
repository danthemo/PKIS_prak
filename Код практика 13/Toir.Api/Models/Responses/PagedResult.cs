namespace Toir.Api.Models.Responses;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int Total, string TraceId);
