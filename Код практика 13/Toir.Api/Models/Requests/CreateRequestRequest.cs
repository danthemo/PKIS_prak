namespace Toir.Api.Models.Requests;

public sealed record CreateRequestRequest(Guid EquipmentId, string Description, string Priority);
