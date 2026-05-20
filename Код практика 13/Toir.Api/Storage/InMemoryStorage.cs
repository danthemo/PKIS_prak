using Toir.Api.Models.Domain;

namespace Toir.Api.Storage;

public sealed class InMemoryStorage
{
    public List<MaintenanceRequest> Requests { get; } = [];
    public List<WorkOrder> WorkOrders { get; } = [];
    public List<Equipment> Equipment { get; } = [];
    public List<AuditEvent> AuditEvents { get; } = [];
    public Dictionary<string, IdempotencyRecord> Idempotency { get; } = new(StringComparer.OrdinalIgnoreCase);
    public object SyncRoot { get; } = new();

    public InMemoryStorage()
    {
        Equipment.AddRange([
            new Equipment
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                InventoryNumber = "ИНВ-001",
                Name = "Станок токарный",
                Location = "Цех 1",
                Status = "Active"
            },
            new Equipment
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                InventoryNumber = "ИНВ-002",
                Name = "Компрессор",
                Location = "Компрессорная",
                Status = "Active"
            },
            new Equipment
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                InventoryNumber = "ИНВ-003",
                Name = "Насосная станция",
                Location = "Участок водоснабжения",
                Status = "Active"
            }
        ]);
    }
}
