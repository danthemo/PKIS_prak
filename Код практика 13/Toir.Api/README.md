# TOiR Client-Server API

Учебный минимальный REST API для подсистемы технического обслуживания и ремонта оборудования. Проект использует ASP.NET Core Minimal API, in-memory-хранилище, упрощенную авторизацию по HTTP-заголовкам и Swagger/OpenAPI.

## Запуск

```powershell
dotnet restore
dotnet build
dotnet run --project .\Toir.Api\Toir.Api.csproj
```

Swagger доступен по адресу:

```text
http://localhost:5000/swagger
```

Если `dotnet run` выберет другой порт, используйте адрес из вывода консоли.

## Авторизация

Каждый запрос к API должен содержать заголовки:

```text
X-User-Id: dispatcher-1
X-User-Role: Dispatcher
```

Доступные роли: `Dispatcher`, `Engineer`, `Chief`, `Admin`, `SecurityOfficer`.

Для трассировки можно передать:

```text
X-Correlation-Id: demo-trace-001
```

## Примеры проверки

### Получить оборудование

```http
GET /api/v1/equipment
X-User-Id: chief-1
X-User-Role: Chief
```

Тестовые `equipmentId`:

```text
11111111-1111-1111-1111-111111111111
22222222-2222-2222-2222-222222222222
33333333-3333-3333-3333-333333333333
```

### Создать заявку

```http
POST /api/v1/requests
X-User-Id: dispatcher-1
X-User-Role: Dispatcher
Content-Type: application/json

{
  "equipmentId": "11111111-1111-1111-1111-111111111111",
  "description": "Не работает привод станка",
  "priority": "High"
}
```

### Назначить инженера и создать наряд

```http
POST /api/v1/requests/{requestId}/assign
X-User-Id: dispatcher-1
X-User-Role: Dispatcher
Content-Type: application/json

{
  "engineerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "operationId": "assign-demo-001"
}
```

Повторный запрос с тем же `operationId` вернет прежний результат и не создаст второй наряд.

### Закрыть наряд

```http
PATCH /api/v1/work-orders/{workOrderId}/close
X-User-Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
X-User-Role: Engineer
Content-Type: application/json

{
  "result": "Ремонт выполнен, оборудование запущено",
  "usedParts": [],
  "operationId": "close-demo-001"
}
```

После закрытия всех нарядов по заявке заявка переводится в статус `Done`.
