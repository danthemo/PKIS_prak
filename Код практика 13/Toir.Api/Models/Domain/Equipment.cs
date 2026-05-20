namespace Toir.Api.Models.Domain;

public sealed class Equipment
{
    public Guid Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
