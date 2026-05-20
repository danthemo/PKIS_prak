namespace IndustrialMaintenance.Domain;

/// <summary>
/// Represents a domain object with a unique identifier.
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    Guid Id { get; }
}

/// <summary>
/// Describes equipment criticality for maintenance prioritization.
/// </summary>
public enum EquipmentCriticality
{
    /// <summary>
    /// Low criticality.
    /// </summary>
    Low,

    /// <summary>
    /// Medium criticality.
    /// </summary>
    Medium,

    /// <summary>
    /// High criticality.
    /// </summary>
    High,

    /// <summary>
    /// Safety-critical equipment.
    /// </summary>
    SafetyCritical
}

/// <summary>
/// Describes the lifecycle state of a work order.
/// </summary>
public enum WorkOrderStatus
{
    /// <summary>
    /// The work order has been created.
    /// </summary>
    Created,

    /// <summary>
    /// The work order has been scheduled.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The work order is being performed.
    /// </summary>
    InProgress,

    /// <summary>
    /// The work order has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The work order has been cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents an exception raised when a domain business rule is violated.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message that describes the violated rule.</param>
    public DomainException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Represents a production facility that owns industrial equipment.
/// </summary>
public sealed class Facility : IIdentifiable
{
    private readonly List<Equipment> _equipment = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Facility"/> class.
    /// </summary>
    /// <param name="name">The facility name.</param>
    /// <exception cref="DomainException">Thrown when the name is empty.</exception>
    public Facility(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Facility name cannot be empty.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    /// <summary>
    /// Gets the facility identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the facility name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the equipment registered at the facility.
    /// </summary>
    public IReadOnlyCollection<Equipment> Equipment => _equipment.AsReadOnly();

    /// <summary>
    /// Adds equipment to the facility and guarantees inventory number uniqueness within it.
    /// </summary>
    /// <param name="inventoryNumber">The equipment inventory number.</param>
    /// <param name="name">The equipment name.</param>
    /// <param name="criticality">The equipment criticality level.</param>
    /// <returns>The created equipment instance.</returns>
    /// <exception cref="DomainException">Thrown when the inventory number is duplicated.</exception>
    public Equipment AddEquipment(string inventoryNumber, string name, EquipmentCriticality criticality)
    {
        if (_equipment.Any(item => string.Equals(item.InventoryNumber, inventoryNumber, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Equipment inventory number must be unique inside a facility.");
        }

        var equipment = new Equipment(inventoryNumber, name, criticality);
        _equipment.Add(equipment);
        return equipment;
    }
}

/// <summary>
/// Represents an industrial equipment unit.
/// </summary>
public sealed class Equipment : IIdentifiable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Equipment"/> class.
    /// </summary>
    /// <param name="inventoryNumber">The inventory number.</param>
    /// <param name="name">The equipment name.</param>
    /// <param name="criticality">The equipment criticality.</param>
    /// <exception cref="DomainException">Thrown when required text fields are empty.</exception>
    internal Equipment(string inventoryNumber, string name, EquipmentCriticality criticality)
    {
        if (string.IsNullOrWhiteSpace(inventoryNumber))
        {
            throw new DomainException("Equipment inventory number cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Equipment name cannot be empty.");
        }

        Id = Guid.NewGuid();
        InventoryNumber = inventoryNumber.Trim();
        Name = name.Trim();
        Criticality = criticality;
    }

    /// <summary>
    /// Gets the equipment identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the inventory number.
    /// </summary>
    public string InventoryNumber { get; }

    /// <summary>
    /// Gets the equipment name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the equipment criticality.
    /// </summary>
    public EquipmentCriticality Criticality { get; }
}

/// <summary>
/// Represents a preventive maintenance plan for equipment.
/// </summary>
public sealed class MaintenancePlan : IIdentifiable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaintenancePlan"/> class.
    /// </summary>
    /// <param name="equipment">The equipment covered by the plan.</param>
    /// <param name="intervalDays">The maintenance interval in days.</param>
    /// <param name="description">The plan description.</param>
    /// <exception cref="DomainException">Thrown when a business rule is violated.</exception>
    public MaintenancePlan(Equipment equipment, int intervalDays, string description)
    {
        Equipment = equipment ?? throw new DomainException("Equipment is required.");

        if (intervalDays is < 1 or > 365)
        {
            throw new DomainException("Maintenance interval must be between 1 and 365 days.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Maintenance plan description cannot be empty.");
        }

        Id = Guid.NewGuid();
        IntervalDays = intervalDays;
        Description = description.Trim();
    }

    /// <summary>
    /// Gets the maintenance plan identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the equipment covered by the plan.
    /// </summary>
    public Equipment Equipment { get; }

    /// <summary>
    /// Gets the maintenance interval in days.
    /// </summary>
    public int IntervalDays { get; }

    /// <summary>
    /// Gets the plan description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Creates a new work order from this maintenance plan.
    /// </summary>
    /// <returns>A created work order linked to the plan.</returns>
    public WorkOrder CreateWorkOrder()
    {
        return new WorkOrder(Equipment, this);
    }
}

/// <summary>
/// Represents a service work order for industrial equipment.
/// </summary>
public sealed class WorkOrder : IIdentifiable
{
    private readonly List<WorkTask> _tasks = [];
    private readonly List<Technician> _technicians = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkOrder"/> class.
    /// </summary>
    /// <param name="equipment">The equipment to service.</param>
    /// <param name="plan">The maintenance plan that created the work order, if any.</param>
    /// <exception cref="DomainException">Thrown when equipment is not provided.</exception>
    public WorkOrder(Equipment equipment, MaintenancePlan? plan = null)
    {
        Equipment = equipment ?? throw new DomainException("Equipment is required.");
        Plan = plan;
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = WorkOrderStatus.Created;
    }

    /// <summary>
    /// Gets the work order identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the equipment to service.
    /// </summary>
    public Equipment Equipment { get; }

    /// <summary>
    /// Gets the maintenance plan that created the work order.
    /// </summary>
    public MaintenancePlan? Plan { get; }

    /// <summary>
    /// Gets the current work order status.
    /// </summary>
    public WorkOrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the work order was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the tasks included in the work order.
    /// </summary>
    public IReadOnlyCollection<WorkTask> Tasks => _tasks.AsReadOnly();

    /// <summary>
    /// Gets the technicians assigned to the work order.
    /// </summary>
    public IReadOnlyCollection<Technician> Technicians => _technicians.AsReadOnly();

    /// <summary>
    /// Adds a task to the work order.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <exception cref="DomainException">Thrown when the task is not provided.</exception>
    public void AddTask(WorkTask task)
    {
        _tasks.Add(task ?? throw new DomainException("Work task is required."));
    }

    /// <summary>
    /// Assigns a technician to the work order.
    /// </summary>
    /// <param name="technician">The technician to assign.</param>
    /// <exception cref="DomainException">Thrown when the technician is not provided.</exception>
    public void AssignTechnician(Technician technician)
    {
        _technicians.Add(technician ?? throw new DomainException("Technician is required."));
    }

    /// <summary>
    /// Changes the work order status from created to scheduled.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the current status does not allow scheduling.</exception>
    public void Schedule()
    {
        EnsureStatus(WorkOrderStatus.Created, "Only a created work order can be scheduled.");
        Status = WorkOrderStatus.Scheduled;
    }

    /// <summary>
    /// Starts the scheduled work order.
    /// </summary>
    /// <exception cref="DomainException">Thrown when start rules are violated.</exception>
    public void Start()
    {
        if (_tasks.Count == 0)
        {
            throw new DomainException("Work order cannot be started without tasks.");
        }

        if (_technicians.Count == 0)
        {
            throw new DomainException("Work order cannot be started without assigned technicians.");
        }

        EnsureStatus(WorkOrderStatus.Scheduled, "Only a scheduled work order can be started.");
        Status = WorkOrderStatus.InProgress;
    }

    /// <summary>
    /// Completes the in-progress work order.
    /// </summary>
    /// <exception cref="DomainException">Thrown when completion rules are violated.</exception>
    public void Complete()
    {
        EnsureStatus(WorkOrderStatus.InProgress, "Only an in-progress work order can be completed.");

        if (_tasks.Any(task => !task.IsCompleted))
        {
            throw new DomainException("All work order tasks must be completed.");
        }

        Status = WorkOrderStatus.Completed;
    }

    /// <summary>
    /// Cancels a created, scheduled, or in-progress work order.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the work order has already been completed.</exception>
    public void Cancel()
    {
        if (Status == WorkOrderStatus.Completed)
        {
            throw new DomainException("Completed work order cannot be cancelled.");
        }

        if (Status == WorkOrderStatus.Cancelled)
        {
            throw new DomainException("Work order is already cancelled.");
        }

        Status = WorkOrderStatus.Cancelled;
    }

    private void EnsureStatus(WorkOrderStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException(message);
        }
    }
}

/// <summary>
/// Represents an abstract task inside a work order.
/// </summary>
public abstract class WorkTask : IIdentifiable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkTask"/> class.
    /// </summary>
    /// <param name="title">The task title.</param>
    /// <param name="estimatedMinutes">The estimated duration in minutes.</param>
    /// <exception cref="DomainException">Thrown when a business rule is violated.</exception>
    protected WorkTask(string title, int estimatedMinutes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Work task title cannot be empty.");
        }

        if (estimatedMinutes <= 0)
        {
            throw new DomainException("Estimated minutes must be greater than zero.");
        }

        Id = Guid.NewGuid();
        Title = title.Trim();
        EstimatedMinutes = estimatedMinutes;
    }

    /// <summary>
    /// Gets the task identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the task title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the estimated task duration in minutes.
    /// </summary>
    public int EstimatedMinutes { get; }

    /// <summary>
    /// Gets a value indicating whether the task is completed.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Marks the task as completed.
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}

/// <summary>
/// Represents an equipment inspection task.
/// </summary>
public sealed class InspectionTask : WorkTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InspectionTask"/> class.
    /// </summary>
    /// <param name="title">The task title.</param>
    /// <param name="estimatedMinutes">The estimated duration in minutes.</param>
    /// <param name="checkPoint">The inspection checkpoint.</param>
    /// <exception cref="DomainException">Thrown when the checkpoint is empty.</exception>
    public InspectionTask(string title, int estimatedMinutes, string checkPoint)
        : base(title, estimatedMinutes)
    {
        if (string.IsNullOrWhiteSpace(checkPoint))
        {
            throw new DomainException("Inspection checkpoint cannot be empty.");
        }

        CheckPoint = checkPoint.Trim();
    }

    /// <summary>
    /// Gets the inspection checkpoint.
    /// </summary>
    public string CheckPoint { get; }
}

/// <summary>
/// Represents an equipment repair task that requires a spare part.
/// </summary>
public sealed class RepairTask : WorkTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepairTask"/> class.
    /// </summary>
    /// <param name="title">The task title.</param>
    /// <param name="estimatedMinutes">The estimated duration in minutes.</param>
    /// <param name="requiredPart">The required spare part.</param>
    /// <param name="requiredQuantity">The required spare part quantity.</param>
    /// <exception cref="DomainException">Thrown when a business rule is violated.</exception>
    public RepairTask(string title, int estimatedMinutes, SparePart requiredPart, int requiredQuantity)
        : base(title, estimatedMinutes)
    {
        RequiredPart = requiredPart ?? throw new DomainException("Required spare part is required.");

        if (requiredQuantity <= 0)
        {
            throw new DomainException("Required quantity must be greater than zero.");
        }

        RequiredQuantity = requiredQuantity;
    }

    /// <summary>
    /// Gets the spare part required for the repair.
    /// </summary>
    public SparePart RequiredPart { get; }

    /// <summary>
    /// Gets the required spare part quantity.
    /// </summary>
    public int RequiredQuantity { get; }
}

/// <summary>
/// Represents a maintenance technician.
/// </summary>
public sealed class Technician : IIdentifiable
{
    private readonly List<string> _skills;

    /// <summary>
    /// Initializes a new instance of the <see cref="Technician"/> class.
    /// </summary>
    /// <param name="fullName">The technician full name.</param>
    /// <param name="skills">The technician skills.</param>
    /// <exception cref="DomainException">Thrown when a business rule is violated.</exception>
    public Technician(string fullName, IEnumerable<string> skills)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Technician full name cannot be empty.");
        }

        _skills = skills?
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .ToList() ?? [];

        if (_skills.Count == 0)
        {
            throw new DomainException("Technician must have at least one skill.");
        }

        Id = Guid.NewGuid();
        FullName = fullName.Trim();
    }

    /// <summary>
    /// Gets the technician identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the technician full name.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the technician skills.
    /// </summary>
    public IReadOnlyCollection<string> Skills => _skills.AsReadOnly();
}

/// <summary>
/// Represents a spare part stored for equipment repairs.
/// </summary>
public sealed class SparePart : IIdentifiable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SparePart"/> class.
    /// </summary>
    /// <param name="article">The spare part article.</param>
    /// <param name="name">The spare part name.</param>
    /// <param name="stockQuantity">The available stock quantity.</param>
    /// <exception cref="DomainException">Thrown when a business rule is violated.</exception>
    public SparePart(string article, string name, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(article))
        {
            throw new DomainException("Spare part article cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Spare part name cannot be empty.");
        }

        if (stockQuantity < 0)
        {
            throw new DomainException("Stock quantity cannot be negative.");
        }

        Id = Guid.NewGuid();
        Article = article.Trim();
        Name = name.Trim();
        StockQuantity = stockQuantity;
    }

    /// <summary>
    /// Gets the spare part identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the spare part article.
    /// </summary>
    public string Article { get; }

    /// <summary>
    /// Gets the spare part name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the available stock quantity.
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Reserves the specified quantity and reduces the stock.
    /// </summary>
    /// <param name="quantity">The quantity to reserve.</param>
    /// <exception cref="DomainException">Thrown when the requested quantity is invalid.</exception>
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Reserved quantity must be greater than zero.");
        }

        if (quantity > StockQuantity)
        {
            throw new DomainException("Reserved quantity cannot exceed stock quantity.");
        }

        StockQuantity -= quantity;
    }
}

/// <summary>
/// Represents a data transfer object for exposing work order summary data.
/// </summary>
public sealed class WorkOrderDto
{
    /// <summary>
    /// Gets or initializes the work order identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the equipment inventory number.
    /// </summary>
    public string EquipmentInventoryNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the equipment name.
    /// </summary>
    public string EquipmentName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the work order status name.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the date and time when the work order was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the number of tasks.
    /// </summary>
    public int TaskCount { get; init; }

    /// <summary>
    /// Gets or initializes the number of assigned technicians.
    /// </summary>
    public int TechnicianCount { get; init; }
}

/// <summary>
/// Maps work order domain objects to DTO contracts.
/// </summary>
public static class WorkOrderMapper
{
    /// <summary>
    /// Converts a work order to a DTO.
    /// </summary>
    /// <param name="workOrder">The work order to convert.</param>
    /// <returns>The DTO representation of the work order.</returns>
    /// <exception cref="DomainException">Thrown when the work order is not provided.</exception>
    public static WorkOrderDto ToDto(WorkOrder workOrder)
    {
        if (workOrder is null)
        {
            throw new DomainException("Work order is required.");
        }

        return new WorkOrderDto
        {
            Id = workOrder.Id,
            EquipmentInventoryNumber = workOrder.Equipment.InventoryNumber,
            EquipmentName = workOrder.Equipment.Name,
            Status = workOrder.Status.ToString(),
            CreatedAt = workOrder.CreatedAt,
            TaskCount = workOrder.Tasks.Count,
            TechnicianCount = workOrder.Technicians.Count
        };
    }
}

/// <summary>
/// Defines persistence operations for work orders.
/// </summary>
public interface IWorkOrderRepository
{
    /// <summary>
    /// Adds a work order to the repository.
    /// </summary>
    /// <param name="workOrder">The work order to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a work order by its identifier.
    /// </summary>
    /// <param name="id">The work order identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The found work order or <see langword="null"/>.</returns>
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
