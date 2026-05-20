namespace Toir.Api.Models.Domain;

public sealed class IdempotencyRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
