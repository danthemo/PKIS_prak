namespace Toir.Api.Models.Requests;

public sealed record AssignEngineerRequest(Guid EngineerId, string OperationId);
