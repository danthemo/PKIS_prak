using Microsoft.OpenApi;
using Toir.Api.Middleware;
using Toir.Api.Models.Requests;
using Toir.Api.Models.Responses;
using Toir.Api.Services;
using Toir.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TOiR Client-Server API",
        Version = "v1",
        Description = "Учебный REST API подсистемы технического обслуживания и ремонта оборудования."
    });
});

builder.Services.AddSingleton<InMemoryStorage>();
builder.Services.AddSingleton<AuthorizationService>();
builder.Services.AddSingleton<ToirService>();

var app = builder.Build();

app.UseMiddleware<TraceLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

var api = app.MapGroup("/api/v1");

api.MapPost("/requests", CreateRequest)
    .WithName("CreateRequest")
    .Produces<ApiResponse<RequestDto>>(StatusCodes.Status201Created)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

api.MapGet("/requests", GetRequests)
    .WithName("GetRequests")
    .Produces<PagedResult<RequestDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

api.MapGet("/requests/{id:guid}", GetRequest)
    .WithName("GetRequest")
    .Produces<ApiResponse<RequestDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

api.MapPatch("/requests/{id:guid}/status", ChangeRequestStatus)
    .WithName("ChangeRequestStatus")
    .Produces<ApiResponse<RequestDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

api.MapPost("/requests/{id:guid}/assign", AssignEngineer)
    .WithName("AssignEngineer")
    .Produces<ApiResponse<WorkOrderDto>>(StatusCodes.Status201Created)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

api.MapGet("/work-orders", GetWorkOrders)
    .WithName("GetWorkOrders")
    .Produces<PagedResult<WorkOrderDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

api.MapPatch("/work-orders/{id:guid}/close", CloseWorkOrder)
    .WithName("CloseWorkOrder")
    .Produces<ApiResponse<WorkOrderDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

api.MapGet("/equipment", GetEquipment)
    .WithName("GetEquipment")
    .Produces<PagedResult<EquipmentDto>>()
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

static IResult CreateRequest(CreateRequestRequest request, ToirService service, HttpContext context)
{
    var result = service.CreateRequest(request, context);
    return Results.Created($"/api/v1/requests/{result.Data.Id}", result);
}

static IResult GetRequests(string? status, Guid? engineerId, int? page, int? pageSize, ToirService service, HttpContext context)
{
    return Results.Ok(service.GetRequests(status, engineerId, page ?? 1, pageSize ?? 20, context));
}

static IResult GetRequest(Guid id, ToirService service, HttpContext context)
{
    return Results.Ok(service.GetRequest(id, context));
}

static IResult ChangeRequestStatus(Guid id, ChangeRequestStatusRequest request, ToirService service, HttpContext context)
{
    return Results.Ok(service.ChangeRequestStatus(id, request, context));
}

static IResult AssignEngineer(Guid id, AssignEngineerRequest request, ToirService service, HttpContext context)
{
    var result = service.AssignEngineer(id, request, context);
    return Results.Created($"/api/v1/work-orders/{result.Data.Id}", result);
}

static IResult GetWorkOrders(string? status, Guid? engineerId, int? page, int? pageSize, ToirService service, HttpContext context)
{
    return Results.Ok(service.GetWorkOrders(status, engineerId, page ?? 1, pageSize ?? 20, context));
}

static IResult CloseWorkOrder(Guid id, CloseWorkOrderRequest request, ToirService service, HttpContext context)
{
    return Results.Ok(service.CloseWorkOrder(id, request, context));
}

static IResult GetEquipment(string? query, int? page, int? pageSize, ToirService service, HttpContext context)
{
    return Results.Ok(service.GetEquipment(query, page ?? 1, pageSize ?? 20, context));
}
