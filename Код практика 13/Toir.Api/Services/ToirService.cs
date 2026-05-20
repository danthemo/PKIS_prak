using System.Text.Json;
using Toir.Api.Models.Domain;
using Toir.Api.Models.Requests;
using Toir.Api.Models.Responses;
using Toir.Api.Storage;

namespace Toir.Api.Services;

public sealed class ToirService(InMemoryStorage storage, AuthorizationService authorization)
{
    private static readonly HashSet<string> Priorities = ["Low", "Normal", "High", "Critical"];
    private static readonly HashSet<string> RequestStatuses = ["Registered", "Assigned", "InProgress", "WaitingParts", "Done", "Closed", "Cancelled"];
    private static readonly HashSet<string> WorkStatuses = ["Assigned", "InProgress", "Closed"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiResponse<RequestDto> CreateRequest(CreateRequestRequest request, HttpContext context)
    {
        var user = authorization.Require(context, "requests:create");
        ValidateCreateRequest(request);

        lock (storage.SyncRoot)
        {
            if (storage.Equipment.All(x => x.Id != request.EquipmentId))
            {
                throw NotFound("Оборудование не найдено.");
            }

            var entity = new MaintenanceRequest
            {
                Id = Guid.NewGuid(),
                Number = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{storage.Requests.Count + 1:0000}",
                EquipmentId = request.EquipmentId,
                Description = request.Description.Trim(),
                Priority = request.Priority,
                Status = "Registered",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserId,
                Version = 1
            };

            storage.Requests.Add(entity);
            AddAudit("CREATE_REQUEST", "MaintenanceRequest", entity.Id, user, context, "Success");
            return Ok(ToDto(entity), context);
        }
    }

    public PagedResult<RequestDto> GetRequests(string? status, Guid? engineerId, int page, int pageSize, HttpContext context)
    {
        authorization.Require(context, "requests:read");
        ValidatePaging(ref page, ref pageSize);

        lock (storage.SyncRoot)
        {
            IEnumerable<MaintenanceRequest> query = storage.Requests;

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (engineerId is not null)
            {
                var requestIds = storage.WorkOrders
                    .Where(x => x.EngineerId == engineerId.Value)
                    .Select(x => x.RequestId)
                    .ToHashSet();

                query = query.Where(x => requestIds.Contains(x.Id));
            }

            return Page(query.OrderByDescending(x => x.CreatedAt).Select(ToDto), page, pageSize, context);
        }
    }

    public ApiResponse<RequestDto> GetRequest(Guid id, HttpContext context)
    {
        authorization.Require(context, "requests:read");

        lock (storage.SyncRoot)
        {
            var entity = storage.Requests.FirstOrDefault(x => x.Id == id) ?? throw NotFound("Заявка не найдена.");
            return Ok(ToDto(entity), context);
        }
    }

    public ApiResponse<RequestDto> ChangeRequestStatus(Guid id, ChangeRequestStatusRequest request, HttpContext context)
    {
        var user = authorization.Require(context, "requests:update");

        if (string.IsNullOrWhiteSpace(request.Status) || !RequestStatuses.Contains(request.Status))
        {
            throw Validation("Некорректный статус заявки.", new("status", "Допустимые значения: Registered, Assigned, InProgress, WaitingParts, Done, Closed, Cancelled."));
        }

        lock (storage.SyncRoot)
        {
            var entity = storage.Requests.FirstOrDefault(x => x.Id == id) ?? throw NotFound("Заявка не найдена.");

            if (!CanMoveRequest(entity.Status, request.Status))
            {
                throw Conflict("Недопустимый переход статуса заявки.");
            }

            entity.Status = request.Status;
            entity.Version++;
            AddAudit("CHANGE_REQUEST_STATUS", "MaintenanceRequest", entity.Id, user, context, "Success");
            return Ok(ToDto(entity), context);
        }
    }

    public ApiResponse<WorkOrderDto> AssignEngineer(Guid requestId, AssignEngineerRequest request, HttpContext context)
    {
        var user = authorization.Require(context, "requests:assign");

        if (request.EngineerId == Guid.Empty)
        {
            throw Validation("Некорректный инженер.", new("engineerId", "Значение engineerId обязательно."));
        }

        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            throw Validation("Некорректный operationId.", new("operationId", "Значение operationId обязательно."));
        }

        lock (storage.SyncRoot)
        {
            var idempotencyKey = IdempotencyKey("ASSIGN_ENGINEER", request.OperationId);
            if (storage.Idempotency.TryGetValue(idempotencyKey, out var record))
            {
                var previous = JsonSerializer.Deserialize<WorkOrderDto>(record.ResultJson, JsonOptions)!;
                return Ok(previous, context);
            }

            var maintenanceRequest = storage.Requests.FirstOrDefault(x => x.Id == requestId) ?? throw NotFound("Заявка не найдена.");

            if (maintenanceRequest.Status != "Registered")
            {
                throw Conflict("Назначить инженера можно только на заявку в статусе Registered.");
            }

            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                EngineerId = request.EngineerId,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow,
                Version = 1
            };

            storage.WorkOrders.Add(workOrder);
            maintenanceRequest.Status = "Assigned";
            maintenanceRequest.Version++;

            var dto = ToDto(workOrder);
            storage.Idempotency[idempotencyKey] = new IdempotencyRecord
            {
                OperationId = request.OperationId,
                Operation = "ASSIGN_ENGINEER",
                ResultJson = JsonSerializer.Serialize(dto, JsonOptions),
                CreatedAt = DateTime.UtcNow
            };

            AddAudit("ASSIGN_ENGINEER", "WorkOrder", workOrder.Id, user, context, "Success");
            return Ok(dto, context);
        }
    }

    public PagedResult<WorkOrderDto> GetWorkOrders(string? status, Guid? engineerId, int page, int pageSize, HttpContext context)
    {
        var user = authorization.Require(context, "workorders:read");
        ValidatePaging(ref page, ref pageSize);

        lock (storage.SyncRoot)
        {
            IEnumerable<WorkOrder> query = storage.WorkOrders;

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (engineerId is not null)
            {
                query = query.Where(x => x.EngineerId == engineerId.Value);
            }

            if (user.Role.Equals("Engineer", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(user.UserId, out var currentEngineerId))
            {
                query = query.Where(x => x.EngineerId == currentEngineerId);
            }

            return Page(query.OrderByDescending(x => x.AssignedAt).Select(ToDto), page, pageSize, context);
        }
    }

    public ApiResponse<WorkOrderDto> CloseWorkOrder(Guid id, CloseWorkOrderRequest request, HttpContext context)
    {
        var user = authorization.Require(context, "workorders:close");

        if (string.IsNullOrWhiteSpace(request.Result))
        {
            throw Validation("Результат закрытия обязателен.", new("result", "Поле result обязательно."));
        }

        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            throw Validation("Некорректный operationId.", new("operationId", "Значение operationId обязательно."));
        }

        if (request.UsedParts is not null && request.UsedParts.Any(x => x.SparePartId == Guid.Empty || x.Quantity <= 0))
        {
            throw Validation("Некорректный список запчастей.", new("usedParts", "SparePartId должен быть заполнен, Quantity должна быть больше 0."));
        }

        lock (storage.SyncRoot)
        {
            var idempotencyKey = IdempotencyKey("CLOSE_WORK_ORDER", request.OperationId);
            if (storage.Idempotency.TryGetValue(idempotencyKey, out var record))
            {
                var previous = JsonSerializer.Deserialize<WorkOrderDto>(record.ResultJson, JsonOptions)!;
                return Ok(previous, context);
            }

            var workOrder = storage.WorkOrders.FirstOrDefault(x => x.Id == id) ?? throw NotFound("Наряд не найден.");

            if (workOrder.Status == "Closed")
            {
                throw Conflict("Наряд уже закрыт.");
            }

            if (!WorkStatuses.Contains(workOrder.Status))
            {
                throw Conflict("Текущий статус наряда не позволяет закрытие.");
            }

            workOrder.Status = "Closed";
            workOrder.ClosedAt = DateTime.UtcNow;
            workOrder.Result = request.Result.Trim();
            workOrder.Version++;

            var relatedRequest = storage.Requests.FirstOrDefault(x => x.Id == workOrder.RequestId);
            if (relatedRequest is not null && storage.WorkOrders.Where(x => x.RequestId == relatedRequest.Id).All(x => x.Status == "Closed"))
            {
                relatedRequest.Status = "Done";
                relatedRequest.Version++;
            }

            var dto = ToDto(workOrder);
            storage.Idempotency[idempotencyKey] = new IdempotencyRecord
            {
                OperationId = request.OperationId,
                Operation = "CLOSE_WORK_ORDER",
                ResultJson = JsonSerializer.Serialize(dto, JsonOptions),
                CreatedAt = DateTime.UtcNow
            };

            AddAudit("CLOSE_WORK_ORDER", "WorkOrder", workOrder.Id, user, context, "Success");
            return Ok(dto, context);
        }
    }

    public PagedResult<EquipmentDto> GetEquipment(string? query, int page, int pageSize, HttpContext context)
    {
        authorization.Require(context, "equipment:read");
        ValidatePaging(ref page, ref pageSize);

        lock (storage.SyncRoot)
        {
            IEnumerable<Equipment> items = storage.Equipment;

            if (!string.IsNullOrWhiteSpace(query))
            {
                items = items.Where(x =>
                    x.InventoryNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || x.Location.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return Page(items.OrderBy(x => x.InventoryNumber).Select(ToDto), page, pageSize, context);
        }
    }

    private void AddAudit(string action, string entityType, Guid entityId, CurrentUser user, HttpContext context, string result)
    {
        storage.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = user.UserId,
            Role = user.Role,
            OccurredAt = DateTime.UtcNow,
            TraceId = context.GetTraceId(),
            Result = result
        });
    }

    private static ApiResponse<T> Ok<T>(T data, HttpContext context)
    {
        return new ApiResponse<T>(data, context.GetTraceId(), DateTime.UtcNow);
    }

    private static PagedResult<T> Page<T>(IEnumerable<T> source, int page, int pageSize, HttpContext context)
    {
        var list = source.ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(items, page, pageSize, list.Count, context.GetTraceId());
    }

    private static void ValidatePaging(ref int page, ref int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
    }

    private static void ValidateCreateRequest(CreateRequestRequest request)
    {
        var errors = new List<FieldError>();

        if (request.EquipmentId == Guid.Empty)
        {
            errors.Add(new FieldError("equipmentId", "Значение equipmentId обязательно."));
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length < 5)
        {
            errors.Add(new FieldError("description", "Описание обязательно и должно содержать минимум 5 символов."));
        }

        if (string.IsNullOrWhiteSpace(request.Priority) || !Priorities.Contains(request.Priority))
        {
            errors.Add(new FieldError("priority", "Допустимые значения: Low, Normal, High, Critical."));
        }

        if (errors.Count > 0)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "Ошибка валидации входных данных.", errors);
        }
    }

    private static bool CanMoveRequest(string current, string next)
    {
        if (current == next)
        {
            return true;
        }

        if (current == "Closed" && next == "InProgress")
        {
            return false;
        }

        if (current == "Cancelled" && next is "Assigned" or "InProgress" or "WaitingParts" or "Done" or "Closed")
        {
            return false;
        }

        if (current == "Registered" && next == "Closed")
        {
            return false;
        }

        return true;
    }

    private static RequestDto ToDto(MaintenanceRequest request)
    {
        return new RequestDto(request.Id, request.Number, request.EquipmentId, request.Description, request.Priority, request.Status, request.CreatedAt, request.CreatedBy, request.Version);
    }

    private static WorkOrderDto ToDto(WorkOrder workOrder)
    {
        return new WorkOrderDto(workOrder.Id, workOrder.RequestId, workOrder.EngineerId, workOrder.Status, workOrder.AssignedAt, workOrder.ClosedAt, workOrder.Result, workOrder.Version);
    }

    private static EquipmentDto ToDto(Equipment equipment)
    {
        return new EquipmentDto(equipment.Id, equipment.InventoryNumber, equipment.Name, equipment.Location, equipment.Status);
    }

    private static ApiException NotFound(string message)
    {
        return new ApiException(StatusCodes.Status404NotFound, "NOT_FOUND", message);
    }

    private static ApiException Conflict(string message)
    {
        return new ApiException(StatusCodes.Status409Conflict, "STATE_CONFLICT", message);
    }

    private static ApiException Validation(string message, FieldError detail)
    {
        return new ApiException(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", message, [detail]);
    }

    private static string IdempotencyKey(string operation, string operationId)
    {
        return $"{operation}:{operationId.Trim()}";
    }
}
