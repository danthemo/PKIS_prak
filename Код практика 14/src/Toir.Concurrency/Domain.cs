using System.Text.Json;

namespace Toir.Concurrency;

/// <summary>State of a maintenance request.</summary>
public enum RequestStatus
{
    /// <summary>The request is registered and waits for assignment.</summary>
    Registered,
    /// <summary>The request has an assigned work order.</summary>
    Assigned,
    /// <summary>The request is being processed.</summary>
    InProgress,
    /// <summary>The request is blocked by missing spare parts.</summary>
    WaitingParts,
    /// <summary>All required work is done.</summary>
    Done,
    /// <summary>The request is closed.</summary>
    Closed,
    /// <summary>The request is cancelled.</summary>
    Cancelled
}

/// <summary>State of a work order.</summary>
public enum WorkOrderStatus
{
    /// <summary>The work order is assigned to an engineer.</summary>
    Assigned,
    /// <summary>The work order is in progress.</summary>
    InProgress,
    /// <summary>The work order is completed but not closed.</summary>
    Completed,
    /// <summary>The work order is closed.</summary>
    Closed,
    /// <summary>The work order is cancelled.</summary>
    Cancelled
}

/// <summary>Error codes returned by application services.</summary>
public enum ErrorCode
{
    /// <summary>No error occurred.</summary>
    None,
    /// <summary>Input data is invalid.</summary>
    ValidationError,
    /// <summary>The requested entity was not found.</summary>
    NotFound,
    /// <summary>The current state or version conflicts with the operation.</summary>
    StateConflict,
    /// <summary>The operation is forbidden by business rules.</summary>
    Forbidden,
    /// <summary>An unexpected internal error occurred.</summary>
    InternalError
}

/// <summary>Maintenance request aggregate.</summary>
public sealed class MaintenanceRequest
{
    /// <summary>Request identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable request number.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Equipment identifier.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Current request status.</summary>
    public RequestStatus Status { get; set; }

    /// <summary>Optimistic locking version.</summary>
    public int Version { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Related work order identifiers.</summary>
    public List<Guid> WorkOrderIds { get; set; } = [];
}

/// <summary>Work order assigned to an engineer.</summary>
public sealed class WorkOrder
{
    /// <summary>Work order identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent request identifier.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Assigned engineer identifier.</summary>
    public Guid EngineerId { get; set; }

    /// <summary>Current work order status.</summary>
    public WorkOrderStatus Status { get; set; }

    /// <summary>Closure result text.</summary>
    public string? Result { get; set; }

    /// <summary>Optimistic locking version.</summary>
    public int Version { get; set; }

    /// <summary>Assignment timestamp.</summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>Closure timestamp.</summary>
    public DateTime? ClosedAt { get; set; }
}

/// <summary>Spare part stock item.</summary>
public sealed class SparePart
{
    /// <summary>Spare part identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Article code.</summary>
    public string Article { get; set; } = string.Empty;

    /// <summary>Part name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Available quantity.</summary>
    public decimal StockQuantity { get; set; }

    /// <summary>Optimistic locking version.</summary>
    public int Version { get; set; }
}

/// <summary>Successful spare part reservation.</summary>
public sealed class SparePartReservation
{
    /// <summary>Reservation identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Reserved spare part identifier.</summary>
    public Guid SparePartId { get; set; }

    /// <summary>Reserved quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Reservation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Audit event for a critical operation.</summary>
public sealed class AuditEvent
{
    /// <summary>Audit event identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Action name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity type name.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity identifier.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Operation result summary.</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>Trace identifier.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Event timestamp.</summary>
    public DateTime OccurredAt { get; set; }
}

/// <summary>Persisted idempotent operation result.</summary>
public sealed class IdempotencyRecord
{
    /// <summary>Unique client operation identifier.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Operation name.</summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Serialized operation result.</summary>
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Generic operation result.</summary>
public sealed class OperationResult<T>
{
    /// <summary>Indicates whether the operation completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Operation payload.</summary>
    public T? Data { get; set; }

    /// <summary>Error code.</summary>
    public ErrorCode ErrorCode { get; set; }

    /// <summary>Human-readable message.</summary>
    public string? Message { get; set; }

    /// <summary>Trace identifier.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Creates a successful result.</summary>
    public static OperationResult<T> Ok(T data, string traceId) =>
        new() { Success = true, Data = data, ErrorCode = ErrorCode.None, TraceId = traceId };

    /// <summary>Creates a failed result.</summary>
    public static OperationResult<T> Fail(ErrorCode code, string message, string traceId) =>
        new() { Success = false, ErrorCode = code, Message = message, TraceId = traceId };
}

/// <summary>Assignment operation payload.</summary>
public sealed record AssignEngineerResult(Guid RequestId, Guid WorkOrderId, Guid EngineerId, int RequestVersion);

/// <summary>Work order close operation payload.</summary>
public sealed record CloseWorkOrderResult(Guid WorkOrderId, WorkOrderStatus Status, int WorkOrderVersion);

/// <summary>Spare part reservation operation payload.</summary>
public sealed record ReservePartResult(Guid ReservationId, Guid SparePartId, decimal ReservedQuantity, decimal RemainingQuantity, int SparePartVersion);

/// <summary>Raised when a domain state conflict is detected.</summary>
public sealed class DomainConflictException(string message) : Exception(message);

/// <summary>Raised when input validation fails.</summary>
public sealed class ValidationException(string message) : Exception(message);

/// <summary>Thread-safe in-memory storage that executes critical sections atomically.</summary>
public sealed class InMemoryTransactionalStorage
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, MaintenanceRequest> requests = [];
    private readonly Dictionary<Guid, WorkOrder> workOrders = [];
    private readonly Dictionary<Guid, SparePart> spareParts = [];
    private readonly Dictionary<Guid, SparePartReservation> reservations = [];
    private readonly List<AuditEvent> auditEvents = [];
    private readonly Dictionary<string, IdempotencyRecord> idempotency = [];

    /// <summary>Runs a function inside the storage transaction lock.</summary>
    public async Task<T> ExecuteAsync<T>(Func<StorageTransaction, T> action)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return action(new StorageTransaction(requests, workOrders, spareParts, reservations, auditEvents, idempotency));
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Adds a request for tests or setup code.</summary>
    public Task AddRequestAsync(MaintenanceRequest request) => ExecuteAsync(tx =>
    {
        tx.Requests[request.Id] = Clone(request);
        return true;
    });

    /// <summary>Adds a work order for tests or setup code.</summary>
    public Task AddWorkOrderAsync(WorkOrder workOrder) => ExecuteAsync(tx =>
    {
        tx.WorkOrders[workOrder.Id] = Clone(workOrder);
        if (tx.Requests.TryGetValue(workOrder.RequestId, out var request) && !request.WorkOrderIds.Contains(workOrder.Id))
        {
            request.WorkOrderIds.Add(workOrder.Id);
        }

        return true;
    });

    /// <summary>Adds a spare part for tests or setup code.</summary>
    public Task AddSparePartAsync(SparePart sparePart) => ExecuteAsync(tx =>
    {
        tx.SpareParts[sparePart.Id] = Clone(sparePart);
        return true;
    });

    /// <summary>Returns a request snapshot.</summary>
    public Task<MaintenanceRequest?> GetRequestAsync(Guid id) => ExecuteAsync(tx =>
        tx.Requests.TryGetValue(id, out var request) ? Clone(request) : null);

    /// <summary>Returns a work order snapshot.</summary>
    public Task<WorkOrder?> GetWorkOrderAsync(Guid id) => ExecuteAsync(tx =>
        tx.WorkOrders.TryGetValue(id, out var order) ? Clone(order) : null);

    /// <summary>Returns a spare part snapshot.</summary>
    public Task<SparePart?> GetSparePartAsync(Guid id) => ExecuteAsync(tx =>
        tx.SpareParts.TryGetValue(id, out var part) ? Clone(part) : null);

    /// <summary>Returns all work order snapshots.</summary>
    public Task<IReadOnlyList<WorkOrder>> GetWorkOrdersAsync() => ExecuteAsync(tx =>
        (IReadOnlyList<WorkOrder>)tx.WorkOrders.Values.Select(Clone).ToList());

    /// <summary>Returns all audit event snapshots.</summary>
    public Task<IReadOnlyList<AuditEvent>> GetAuditEventsAsync() => ExecuteAsync(tx =>
        (IReadOnlyList<AuditEvent>)tx.AuditEvents.Select(Clone).ToList());

    internal static MaintenanceRequest Clone(MaintenanceRequest source) => new()
    {
        Id = source.Id,
        Number = source.Number,
        EquipmentId = source.EquipmentId,
        Status = source.Status,
        Version = source.Version,
        CreatedAt = source.CreatedAt,
        WorkOrderIds = [.. source.WorkOrderIds]
    };

    internal static WorkOrder Clone(WorkOrder source) => new()
    {
        Id = source.Id,
        RequestId = source.RequestId,
        EngineerId = source.EngineerId,
        Status = source.Status,
        Result = source.Result,
        Version = source.Version,
        AssignedAt = source.AssignedAt,
        ClosedAt = source.ClosedAt
    };

    internal static SparePart Clone(SparePart source) => new()
    {
        Id = source.Id,
        Article = source.Article,
        Name = source.Name,
        StockQuantity = source.StockQuantity,
        Version = source.Version
    };

    private static AuditEvent Clone(AuditEvent source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        Result = source.Result,
        TraceId = source.TraceId,
        OccurredAt = source.OccurredAt
    };
}

/// <summary>Mutable transaction view used only inside the storage lock.</summary>
public sealed class StorageTransaction(
    Dictionary<Guid, MaintenanceRequest> requests,
    Dictionary<Guid, WorkOrder> workOrders,
    Dictionary<Guid, SparePart> spareParts,
    Dictionary<Guid, SparePartReservation> reservations,
    List<AuditEvent> auditEvents,
    Dictionary<string, IdempotencyRecord> idempotency)
{
    /// <summary>Request table.</summary>
    public Dictionary<Guid, MaintenanceRequest> Requests { get; } = requests;

    /// <summary>Work order table.</summary>
    public Dictionary<Guid, WorkOrder> WorkOrders { get; } = workOrders;

    /// <summary>Spare part table.</summary>
    public Dictionary<Guid, SparePart> SpareParts { get; } = spareParts;

    /// <summary>Reservation table.</summary>
    public Dictionary<Guid, SparePartReservation> Reservations { get; } = reservations;

    /// <summary>Audit event table.</summary>
    public List<AuditEvent> AuditEvents { get; } = auditEvents;

    /// <summary>Idempotency table.</summary>
    public Dictionary<string, IdempotencyRecord> Idempotency { get; } = idempotency;
}

/// <summary>Writes audit events.</summary>
public sealed class AuditService(InMemoryTransactionalStorage storage)
{
    /// <summary>Returns all audit events.</summary>
    public Task<IReadOnlyList<AuditEvent>> GetEventsAsync() => storage.GetAuditEventsAsync();
}

/// <summary>Reads idempotency records.</summary>
public sealed class IdempotencyService(InMemoryTransactionalStorage storage)
{
    /// <summary>Returns an idempotency record by operation identifier.</summary>
    public Task<IdempotencyRecord?> GetAsync(string operationId) => storage.ExecuteAsync(tx =>
        tx.Idempotency.TryGetValue(operationId, out var record)
            ? new IdempotencyRecord { OperationId = record.OperationId, OperationName = record.OperationName, ResultJson = record.ResultJson, CreatedAt = record.CreatedAt }
            : null);
}

/// <summary>Application service for maintenance requests.</summary>
public sealed class RequestService(InMemoryTransactionalStorage storage)
{
    /// <summary>Assigns an engineer and creates one work order for a registered request.</summary>
    public Task<OperationResult<AssignEngineerResult>> AssignEngineerAsync(Guid requestId, Guid engineerId, string operationId, string traceId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Task.FromResult(OperationResult<AssignEngineerResult>.Fail(ErrorCode.ValidationError, "operationId is required.", traceId));
        }

        return storage.ExecuteAsync(tx =>
        {
            if (TryGetIdempotent<AssignEngineerResult>(tx, operationId, out var saved))
            {
                saved.TraceId = traceId;
                return saved;
            }

            if (!tx.Requests.TryGetValue(requestId, out var request))
            {
                return OperationResult<AssignEngineerResult>.Fail(ErrorCode.NotFound, "Request was not found.", traceId);
            }

            if (request.Status != RequestStatus.Registered)
            {
                return OperationResult<AssignEngineerResult>.Fail(ErrorCode.StateConflict, "Request is not registered.", traceId);
            }

            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                EngineerId = engineerId,
                Status = WorkOrderStatus.Assigned,
                Version = 1,
                AssignedAt = DateTime.UtcNow
            };

            request.Status = RequestStatus.Assigned;
            request.Version++;
            request.WorkOrderIds.Add(workOrder.Id);
            tx.WorkOrders[workOrder.Id] = workOrder;
            AddAudit(tx, "AssignEngineer", nameof(MaintenanceRequest), request.Id, "Success", traceId);

            var result = OperationResult<AssignEngineerResult>.Ok(new AssignEngineerResult(request.Id, workOrder.Id, engineerId, request.Version), traceId);
            SaveIdempotent(tx, operationId, nameof(AssignEngineerAsync), result);
            return result;
        });
    }

    /// <summary>Recalculates a request status from its work orders.</summary>
    public Task<OperationResult<RequestStatus>> RecalculateRequestStatusAsync(Guid requestId, string traceId) => storage.ExecuteAsync(tx =>
    {
        if (!tx.Requests.TryGetValue(requestId, out var request))
        {
            return OperationResult<RequestStatus>.Fail(ErrorCode.NotFound, "Request was not found.", traceId);
        }

        var orders = request.WorkOrderIds.Select(id => tx.WorkOrders[id]).ToList();
        var hasActiveOrders = orders.Any(order => order.Status is not WorkOrderStatus.Closed and not WorkOrderStatus.Cancelled);
        var newStatus = hasActiveOrders ? (request.Status == RequestStatus.Registered ? RequestStatus.Assigned : request.Status) : RequestStatus.Done;

        if (request.Status == newStatus)
        {
            return OperationResult<RequestStatus>.Ok(request.Status, traceId);
        }

        request.Status = newStatus;
        request.Version++;
        AddAudit(tx, "RecalculateRequestStatus", nameof(MaintenanceRequest), request.Id, $"Status={newStatus}", traceId);
        return OperationResult<RequestStatus>.Ok(request.Status, traceId);
    });

    /// <summary>Changes a request status using optimistic locking.</summary>
    public Task<OperationResult<RequestStatus>> ChangeRequestStatusAsync(Guid requestId, RequestStatus newStatus, int expectedVersion, string traceId) => storage.ExecuteAsync(tx =>
    {
        if (!tx.Requests.TryGetValue(requestId, out var request))
        {
            return OperationResult<RequestStatus>.Fail(ErrorCode.NotFound, "Request was not found.", traceId);
        }

        if (request.Version != expectedVersion)
        {
            return OperationResult<RequestStatus>.Fail(ErrorCode.StateConflict, "Request version conflict.", traceId);
        }

        if ((request.Status == RequestStatus.Closed && newStatus == RequestStatus.InProgress)
            || (request.Status == RequestStatus.Cancelled && newStatus == RequestStatus.InProgress)
            || (request.Status == RequestStatus.Registered && newStatus == RequestStatus.Closed))
        {
            return OperationResult<RequestStatus>.Fail(ErrorCode.StateConflict, "Forbidden status transition.", traceId);
        }

        if (request.Status != newStatus)
        {
            request.Status = newStatus;
            request.Version++;
            AddAudit(tx, "ChangeRequestStatus", nameof(MaintenanceRequest), request.Id, $"Status={newStatus}", traceId);
        }

        return OperationResult<RequestStatus>.Ok(request.Status, traceId);
    });

    internal static void AddAudit(StorageTransaction tx, string action, string entityType, Guid entityId, string result, string traceId) =>
        tx.AuditEvents.Add(new AuditEvent { Id = Guid.NewGuid(), Action = action, EntityType = entityType, EntityId = entityId, Result = result, TraceId = traceId, OccurredAt = DateTime.UtcNow });

    internal static bool TryGetIdempotent<T>(StorageTransaction tx, string operationId, out OperationResult<T> result)
    {
        if (tx.Idempotency.TryGetValue(operationId, out var record))
        {
            result = JsonSerializer.Deserialize<OperationResult<T>>(record.ResultJson) ?? OperationResult<T>.Fail(ErrorCode.InternalError, "Saved result cannot be read.", string.Empty);
            return true;
        }

        result = null!;
        return false;
    }

    internal static void SaveIdempotent<T>(StorageTransaction tx, string operationId, string operationName, OperationResult<T> result) =>
        tx.Idempotency[operationId] = new IdempotencyRecord { OperationId = operationId, OperationName = operationName, ResultJson = JsonSerializer.Serialize(result), CreatedAt = DateTime.UtcNow };
}

/// <summary>Application service for work orders.</summary>
public sealed class WorkOrderService(InMemoryTransactionalStorage storage)
{
    /// <summary>Closes a work order once and stores an idempotent result.</summary>
    public Task<OperationResult<CloseWorkOrderResult>> CloseWorkOrderAsync(Guid workOrderId, string result, string operationId, string traceId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Task.FromResult(OperationResult<CloseWorkOrderResult>.Fail(ErrorCode.ValidationError, "operationId is required.", traceId));
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            return Task.FromResult(OperationResult<CloseWorkOrderResult>.Fail(ErrorCode.ValidationError, "result is required.", traceId));
        }

        return storage.ExecuteAsync(tx =>
        {
            if (RequestService.TryGetIdempotent<CloseWorkOrderResult>(tx, operationId, out var saved))
            {
                saved.TraceId = traceId;
                return saved;
            }

            if (!tx.WorkOrders.TryGetValue(workOrderId, out var workOrder))
            {
                return OperationResult<CloseWorkOrderResult>.Fail(ErrorCode.NotFound, "Work order was not found.", traceId);
            }

            if (workOrder.Status == WorkOrderStatus.Closed)
            {
                return OperationResult<CloseWorkOrderResult>.Fail(ErrorCode.StateConflict, "Work order is already closed.", traceId);
            }

            workOrder.Status = WorkOrderStatus.Closed;
            workOrder.Result = result;
            workOrder.ClosedAt = DateTime.UtcNow;
            workOrder.Version++;
            RequestService.AddAudit(tx, "CloseWorkOrder", nameof(WorkOrder), workOrder.Id, "Success", traceId);

            var payload = new CloseWorkOrderResult(workOrder.Id, workOrder.Status, workOrder.Version);
            var operationResult = OperationResult<CloseWorkOrderResult>.Ok(payload, traceId);
            RequestService.SaveIdempotent(tx, operationId, nameof(CloseWorkOrderAsync), operationResult);
            return operationResult;
        });
    }
}

/// <summary>Application service for spare parts.</summary>
public sealed class SparePartService(InMemoryTransactionalStorage storage)
{
    /// <summary>Reserves spare parts if enough stock is available.</summary>
    public Task<OperationResult<ReservePartResult>> ReservePartAsync(Guid sparePartId, decimal quantity, string traceId)
    {
        if (quantity <= 0)
        {
            return Task.FromResult(OperationResult<ReservePartResult>.Fail(ErrorCode.ValidationError, "quantity must be greater than zero.", traceId));
        }

        return storage.ExecuteAsync(tx =>
        {
            if (!tx.SpareParts.TryGetValue(sparePartId, out var part))
            {
                return OperationResult<ReservePartResult>.Fail(ErrorCode.NotFound, "Spare part was not found.", traceId);
            }

            if (part.StockQuantity < quantity)
            {
                return OperationResult<ReservePartResult>.Fail(ErrorCode.StateConflict, "Not enough stock.", traceId);
            }

            part.StockQuantity -= quantity;
            part.Version++;
            var reservation = new SparePartReservation { Id = Guid.NewGuid(), SparePartId = sparePartId, Quantity = quantity, CreatedAt = DateTime.UtcNow };
            tx.Reservations[reservation.Id] = reservation;
            RequestService.AddAudit(tx, "ReservePart", nameof(SparePart), part.Id, "Success", traceId);

            var payload = new ReservePartResult(reservation.Id, part.Id, quantity, part.StockQuantity, part.Version);
            return OperationResult<ReservePartResult>.Ok(payload, traceId);
        });
    }
}
