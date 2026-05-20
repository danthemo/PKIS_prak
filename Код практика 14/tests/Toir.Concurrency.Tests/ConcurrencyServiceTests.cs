using Toir.Concurrency;

namespace Toir.Concurrency.Tests;

/// <summary>Tests for transactional and concurrent behavior.</summary>
public sealed class ConcurrencyServiceTests
{
    [Fact]
    public async Task ConcurrentAssignEngineer_AllowsOnlyOneWorkOrder()
    {
        var context = await CreateContextAsync(RequestStatus.Registered);
        var requestId = context.RequestId;

        var results = await RunConcurrently(
            () => context.RequestService.AssignEngineerAsync(requestId, Guid.NewGuid(), "op-1", "t1"),
            () => context.RequestService.AssignEngineerAsync(requestId, Guid.NewGuid(), "op-2", "t2"));

        var orders = await context.Storage.GetWorkOrdersAsync();
        var request = await context.Storage.GetRequestAsync(requestId);

        Assert.Single(orders);
        Assert.Equal(RequestStatus.Assigned, request!.Status);
        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => result.ErrorCode == ErrorCode.StateConflict);
    }

    [Fact]
    public async Task ConcurrentAssignEngineer_WithSameOperationId_IsIdempotent()
    {
        var context = await CreateContextAsync(RequestStatus.Registered);

        var results = await RunConcurrently(
            () => context.RequestService.AssignEngineerAsync(context.RequestId, Guid.NewGuid(), "same-op", "t1"),
            () => context.RequestService.AssignEngineerAsync(context.RequestId, Guid.NewGuid(), "same-op", "t2"));

        var orders = await context.Storage.GetWorkOrdersAsync();

        Assert.All(results, result => Assert.True(result.Success));
        Assert.Single(orders);
        Assert.Equal(results[0].Data!.WorkOrderId, results[1].Data!.WorkOrderId);
    }

    [Fact]
    public async Task ConcurrentCloseWorkOrder_AllowsOnlyOneClose()
    {
        var context = await CreateContextAsync(RequestStatus.InProgress);
        var workOrder = await AddWorkOrderAsync(context, WorkOrderStatus.InProgress);

        var results = await RunConcurrently(
            () => context.WorkOrderService.CloseWorkOrderAsync(workOrder.Id, "done", "close-1", "t1"),
            () => context.WorkOrderService.CloseWorkOrderAsync(workOrder.Id, "done", "close-2", "t2"));

        var stored = await context.Storage.GetWorkOrderAsync(workOrder.Id);
        var audit = await context.AuditService.GetEventsAsync();

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => result.ErrorCode == ErrorCode.StateConflict);
        Assert.Equal(WorkOrderStatus.Closed, stored!.Status);
        Assert.Single(audit, entry => entry.Action == "CloseWorkOrder");
    }

    [Fact]
    public async Task ConcurrentCloseWorkOrder_WithSameOperationId_IsIdempotent()
    {
        var context = await CreateContextAsync(RequestStatus.InProgress);
        var workOrder = await AddWorkOrderAsync(context, WorkOrderStatus.InProgress);

        var results = await RunConcurrently(
            () => context.WorkOrderService.CloseWorkOrderAsync(workOrder.Id, "done", "same-close", "t1"),
            () => context.WorkOrderService.CloseWorkOrderAsync(workOrder.Id, "done", "same-close", "t2"));

        var audit = await context.AuditService.GetEventsAsync();

        Assert.All(results, result => Assert.True(result.Success));
        Assert.Equal(results[0].Data!.WorkOrderId, results[1].Data!.WorkOrderId);
        Assert.Equal(results[0].Data!.WorkOrderVersion, results[1].Data!.WorkOrderVersion);
        Assert.Single(audit, entry => entry.Action == "CloseWorkOrder");
    }

    [Fact]
    public async Task ConcurrentReservePart_PreventsNegativeStock()
    {
        var context = CreateContext();
        var sparePart = new SparePart { Id = Guid.NewGuid(), Article = "A-100", Name = "Bearing", StockQuantity = 5, Version = 1 };
        await context.Storage.AddSparePartAsync(sparePart);

        var results = await RunConcurrently(
            () => context.SparePartService.ReservePartAsync(sparePart.Id, 4, "t1"),
            () => context.SparePartService.ReservePartAsync(sparePart.Id, 4, "t2"));

        var stored = await context.Storage.GetSparePartAsync(sparePart.Id);

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => result.ErrorCode is ErrorCode.StateConflict or ErrorCode.ValidationError);
        Assert.True(stored!.StockQuantity >= 0);
        Assert.Equal(1, stored.StockQuantity);
    }

    [Fact]
    public async Task ConcurrentChangeRequestStatus_UsesVersionConflict()
    {
        var context = await CreateContextAsync(RequestStatus.Assigned);
        var request = await context.Storage.GetRequestAsync(context.RequestId);
        var expectedVersion = request!.Version;

        var results = await RunConcurrently(
            () => context.RequestService.ChangeRequestStatusAsync(context.RequestId, RequestStatus.Cancelled, expectedVersion, "t1"),
            () => context.RequestService.ChangeRequestStatusAsync(context.RequestId, RequestStatus.InProgress, expectedVersion, "t2"));

        var stored = await context.Storage.GetRequestAsync(context.RequestId);

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => result.ErrorCode == ErrorCode.StateConflict);
        Assert.Equal(expectedVersion + 1, stored!.Version);
    }

    [Fact]
    public async Task AssignEngineer_UnknownRequest_ReturnsNotFound()
    {
        var context = CreateContext();

        var result = await context.RequestService.AssignEngineerAsync(Guid.NewGuid(), Guid.NewGuid(), "op", "trace");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task AssignEngineer_RequestNotRegistered_ReturnsStateConflict()
    {
        var context = await CreateContextAsync(RequestStatus.Assigned);

        var result = await context.RequestService.AssignEngineerAsync(context.RequestId, Guid.NewGuid(), "op", "trace");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.StateConflict, result.ErrorCode);
    }

    [Fact]
    public async Task CloseWorkOrder_EmptyResult_ReturnsValidationError()
    {
        var context = await CreateContextAsync(RequestStatus.InProgress);
        var workOrder = await AddWorkOrderAsync(context, WorkOrderStatus.InProgress);

        var result = await context.WorkOrderService.CloseWorkOrderAsync(workOrder.Id, "", "op", "trace");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
    }

    [Fact]
    public async Task ChangeStatus_ForbiddenTransition_ReturnsStateConflict()
    {
        var context = await CreateContextAsync(RequestStatus.Registered);

        var result = await context.RequestService.ChangeRequestStatusAsync(context.RequestId, RequestStatus.Closed, 1, "trace");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.StateConflict, result.ErrorCode);
    }

    [Fact]
    public async Task ReservePart_ZeroQuantity_ReturnsValidationError()
    {
        var context = CreateContext();

        var result = await context.SparePartService.ReservePartAsync(Guid.NewGuid(), 0, "trace");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
    }

    [Fact]
    public async Task RecalculateRequestStatus_MovesRequestToDone_WhenAllOrdersClosed()
    {
        var context = await CreateContextAsync(RequestStatus.InProgress);
        await AddWorkOrderAsync(context, WorkOrderStatus.Closed);

        var result = await context.RequestService.RecalculateRequestStatusAsync(context.RequestId, "trace");
        var request = await context.Storage.GetRequestAsync(context.RequestId);

        Assert.True(result.Success);
        Assert.Equal(RequestStatus.Done, result.Data);
        Assert.Equal(RequestStatus.Done, request!.Status);
    }

    [Fact]
    public async Task RecalculateRequestStatus_DoesNotMoveToDone_WhenActiveOrdersExist()
    {
        var context = await CreateContextAsync(RequestStatus.InProgress);
        await AddWorkOrderAsync(context, WorkOrderStatus.InProgress);

        var result = await context.RequestService.RecalculateRequestStatusAsync(context.RequestId, "trace");
        var request = await context.Storage.GetRequestAsync(context.RequestId);

        Assert.True(result.Success);
        Assert.Equal(RequestStatus.InProgress, result.Data);
        Assert.Equal(RequestStatus.InProgress, request!.Status);
    }

    [Fact]
    public async Task Audit_IsWritten_ForSuccessfulCriticalOperations()
    {
        var context = await CreateContextAsync(RequestStatus.Registered);
        var assign = await context.RequestService.AssignEngineerAsync(context.RequestId, Guid.NewGuid(), "assign", "trace");
        await context.WorkOrderService.CloseWorkOrderAsync(assign.Data!.WorkOrderId, "done", "close", "trace");

        var part = new SparePart { Id = Guid.NewGuid(), Article = "P-1", Name = "Pump", StockQuantity = 10, Version = 1 };
        await context.Storage.AddSparePartAsync(part);
        await context.SparePartService.ReservePartAsync(part.Id, 2, "trace");

        var audit = await context.AuditService.GetEventsAsync();

        Assert.Contains(audit, entry => entry.Action == "AssignEngineer");
        Assert.Contains(audit, entry => entry.Action == "CloseWorkOrder");
        Assert.Contains(audit, entry => entry.Action == "ReservePart");
    }

    private static async Task<WorkOrder> AddWorkOrderAsync(TestContext context, WorkOrderStatus status)
    {
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            RequestId = context.RequestId,
            EngineerId = Guid.NewGuid(),
            Status = status,
            Version = 1,
            AssignedAt = DateTime.UtcNow,
            ClosedAt = status == WorkOrderStatus.Closed ? DateTime.UtcNow : null,
            Result = status == WorkOrderStatus.Closed ? "done" : null
        };

        await context.Storage.AddWorkOrderAsync(workOrder);
        return workOrder;
    }

    private static async Task<TestContext> CreateContextAsync(RequestStatus status)
    {
        var context = CreateContext();
        await context.Storage.AddRequestAsync(new MaintenanceRequest
        {
            Id = context.RequestId,
            Number = "MR-001",
            EquipmentId = Guid.NewGuid(),
            Status = status,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        });

        return context;
    }

    private static TestContext CreateContext()
    {
        var storage = new InMemoryTransactionalStorage();
        return new TestContext(
            Guid.NewGuid(),
            storage,
            new RequestService(storage),
            new WorkOrderService(storage),
            new SparePartService(storage),
            new AuditService(storage));
    }

    private static async Task<T[]> RunConcurrently<T>(params Func<Task<T>>[] operations)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = operations
            .Select(operation => Task.Run(async () =>
            {
                await start.Task;
                return await operation();
            }))
            .ToArray();

        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private sealed record TestContext(
        Guid RequestId,
        InMemoryTransactionalStorage Storage,
        RequestService RequestService,
        WorkOrderService WorkOrderService,
        SparePartService SparePartService,
        AuditService AuditService);
}
