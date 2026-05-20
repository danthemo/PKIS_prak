namespace Toir.Api.Models.Domain;

public sealed class MaintenanceRequest
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int Version { get; set; }
}
