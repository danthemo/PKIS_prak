using IndustrialMaintenance.Domain;

namespace IndustrialMaintenance.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void Facility_IsCreated_WithValidName()
    {
        var facility = new Facility("Assembly shop");

        Assert.NotEqual(Guid.Empty, facility.Id);
        Assert.Equal("Assembly shop", facility.Name);
        Assert.Empty(facility.Equipment);
    }

    [Fact]
    public void Facility_CannotBeCreated_WithEmptyName()
    {
        Assert.Throws<DomainException>(() => new Facility(" "));
    }

    [Fact]
    public void Equipment_IsAdded_ThroughFacilityAddEquipment()
    {
        var facility = new Facility("Compressor station");

        var equipment = facility.AddEquipment("CMP-001", "Main compressor", EquipmentCriticality.High);

        Assert.Contains(equipment, facility.Equipment);
        Assert.Equal("CMP-001", equipment.InventoryNumber);
        Assert.Equal("Main compressor", equipment.Name);
        Assert.Equal(EquipmentCriticality.High, equipment.Criticality);
    }

    [Fact]
    public void Facility_CannotAddEquipment_WithDuplicateInventoryNumber()
    {
        var facility = new Facility("Boiler house");
        facility.AddEquipment("BLR-001", "Boiler", EquipmentCriticality.SafetyCritical);

        Assert.Throws<DomainException>(() =>
            facility.AddEquipment("BLR-001", "Backup boiler", EquipmentCriticality.High));
    }

    [Fact]
    public void Equipment_CannotBeCreated_WithEmptyInventoryNumber()
    {
        var facility = new Facility("Pump station");

        Assert.Throws<DomainException>(() =>
            facility.AddEquipment(" ", "Pump", EquipmentCriticality.Medium));
    }

    [Fact]
    public void Technician_CannotBeCreated_WithoutSkills()
    {
        Assert.Throws<DomainException>(() => new Technician("Ivan Petrov", Array.Empty<string>()));
    }

    [Fact]
    public void SparePart_CannotBeCreated_WithNegativeStockQuantity()
    {
        Assert.Throws<DomainException>(() => new SparePart("BRG-001", "Bearing", -1));
    }

    [Fact]
    public void SparePartReserve_DecreasesStockQuantity()
    {
        var sparePart = new SparePart("FLT-001", "Filter", 10);

        sparePart.Reserve(3);

        Assert.Equal(7, sparePart.StockQuantity);
    }

    [Fact]
    public void SparePartReserve_RejectsNonPositiveQuantity()
    {
        var sparePart = new SparePart("OIL-001", "Oil canister", 5);

        Assert.Throws<DomainException>(() => sparePart.Reserve(0));
    }

    [Fact]
    public void SparePartReserve_RejectsQuantityGreaterThanStock()
    {
        var sparePart = new SparePart("BLT-001", "Bolt", 2);

        Assert.Throws<DomainException>(() => sparePart.Reserve(3));
    }

    [Fact]
    public void MaintenancePlan_CannotBeCreated_WithIntervalDaysLessThanOne()
    {
        var equipment = CreateEquipment();

        Assert.Throws<DomainException>(() => new MaintenancePlan(equipment, 0, "Monthly inspection"));
    }

    [Fact]
    public void MaintenancePlan_CannotBeCreated_WithIntervalDaysGreaterThan365()
    {
        var equipment = CreateEquipment();

        Assert.Throws<DomainException>(() => new MaintenancePlan(equipment, 366, "Annual inspection"));
    }

    [Fact]
    public void MaintenancePlanCreateWorkOrder_CreatesWorkOrder_WithCreatedStatus()
    {
        var equipment = CreateEquipment();
        var plan = new MaintenancePlan(equipment, 30, "Monthly inspection");

        var workOrder = plan.CreateWorkOrder();

        Assert.Equal(WorkOrderStatus.Created, workOrder.Status);
        Assert.Same(equipment, workOrder.Equipment);
        Assert.Same(plan, workOrder.Plan);
    }

    [Fact]
    public void WorkOrder_CannotBeStarted_WithoutTasks()
    {
        var workOrder = CreateWorkOrder();
        workOrder.AssignTechnician(new Technician("Ivan Petrov", ["Electrical"]));
        workOrder.Schedule();

        Assert.Throws<DomainException>(() => workOrder.Start());
    }

    [Fact]
    public void WorkOrder_CannotBeStarted_WithoutTechnician()
    {
        var workOrder = CreateWorkOrder();
        workOrder.AddTask(new InspectionTask("Inspect vibration", 20, "Motor bearing"));
        workOrder.Schedule();

        Assert.Throws<DomainException>(() => workOrder.Start());
    }

    [Fact]
    public void WorkOrder_Transitions_FromCreatedToScheduledToInProgress()
    {
        var workOrder = CreateReadyWorkOrder();

        workOrder.Schedule();
        workOrder.Start();

        Assert.Equal(WorkOrderStatus.InProgress, workOrder.Status);
    }

    [Fact]
    public void WorkOrder_CannotBeCompleted_UntilAllTasksAreCompleted()
    {
        var workOrder = CreateReadyWorkOrder();
        workOrder.Schedule();
        workOrder.Start();

        Assert.Throws<DomainException>(() => workOrder.Complete());
    }

    [Fact]
    public void WorkOrder_Completes_AfterAllTasksAreCompleted()
    {
        var workOrder = CreateReadyWorkOrder();
        workOrder.Schedule();
        workOrder.Start();

        foreach (var task in workOrder.Tasks)
        {
            task.MarkCompleted();
        }

        workOrder.Complete();

        Assert.Equal(WorkOrderStatus.Completed, workOrder.Status);
    }

    [Fact]
    public void WorkOrderDto_IsCreatedByMapper_AndDoesNotExposeDomainCollections()
    {
        var workOrder = CreateReadyWorkOrder();
        workOrder.Schedule();
        var dtoType = typeof(WorkOrderDto);

        var dto = WorkOrderMapper.ToDto(workOrder);

        Assert.Equal(workOrder.Id, dto.Id);
        Assert.Equal(workOrder.Equipment.InventoryNumber, dto.EquipmentInventoryNumber);
        Assert.Equal(workOrder.Equipment.Name, dto.EquipmentName);
        Assert.Equal("Scheduled", dto.Status);
        Assert.Equal(workOrder.CreatedAt, dto.CreatedAt);
        Assert.Equal(1, dto.TaskCount);
        Assert.Equal(1, dto.TechnicianCount);
        Assert.Null(dtoType.GetProperty("Tasks"));
        Assert.Null(dtoType.GetProperty("Technicians"));
    }

    private static Equipment CreateEquipment()
    {
        var facility = new Facility("Test facility");
        return facility.AddEquipment("EQ-001", "Test equipment", EquipmentCriticality.Medium);
    }

    private static WorkOrder CreateWorkOrder()
    {
        return new WorkOrder(CreateEquipment());
    }

    private static WorkOrder CreateReadyWorkOrder()
    {
        var workOrder = CreateWorkOrder();
        workOrder.AddTask(new InspectionTask("Inspect seals", 30, "Seal unit"));
        workOrder.AssignTechnician(new Technician("Ivan Petrov", ["Mechanical"]));
        return workOrder;
    }
}
