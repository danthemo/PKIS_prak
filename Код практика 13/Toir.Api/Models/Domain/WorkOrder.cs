namespace Toir.Api.Models.Domain;

public sealed class WorkOrder
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid EngineerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Result { get; set; }
    public int Version { get; set; }
}
