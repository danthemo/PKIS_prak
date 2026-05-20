namespace Toir.Api.Models.Responses;

public sealed record UsedPartDto(Guid SparePartId, decimal Quantity);

public sealed record RequestDto(
    Guid Id,
    string Number,
    Guid EquipmentId,
    string Description,
    string Priority,
    string Status,
    DateTime CreatedAt,
    string CreatedBy,
    int Version);

public sealed record WorkOrderDto(
    Guid Id,
    Guid RequestId,
    Guid EngineerId,
    string Status,
    DateTime AssignedAt,
    DateTime? ClosedAt,
    string? Result,
    int Version);

public sealed record EquipmentDto(
    Guid Id,
    string InventoryNumber,
    string Name,
    string Location,
    string Status);
