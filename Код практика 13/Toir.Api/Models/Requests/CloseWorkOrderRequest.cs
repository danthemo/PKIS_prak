using Toir.Api.Models.Responses;

namespace Toir.Api.Models.Requests;

public sealed record CloseWorkOrderRequest(string Result, List<UsedPartDto>? UsedParts, string OperationId);
