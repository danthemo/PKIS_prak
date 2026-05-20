namespace Toir.Api.Models.Domain;

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}
