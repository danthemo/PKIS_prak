insert into toir.users (id, login, full_name, role_name)
values
    ('11111111-1111-1111-1111-111111111111', 'dispatcher', 'Dispatch Operator', 'Dispatcher'),
    ('22222222-2222-2222-2222-222222222222', 'engineer', 'Maintenance Engineer', 'Engineer'),
    ('33333333-3333-3333-3333-333333333333', 'chief', 'Chief Engineer', 'Chief')
on conflict (login) do update
set full_name = excluded.full_name,
    role_name = excluded.role_name;

insert into toir.equipment (id, inventory_number, name, location, status)
values
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'ИНВ-001', 'Станок токарный', 'Цех 1', 'Active'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'ИНВ-002', 'Компрессор', 'Компрессорная', 'Active'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', 'ИНВ-003', 'Насосная станция', 'Участок водоснабжения', 'Inactive')
on conflict (inventory_number) do update
set name = excluded.name,
    location = excluded.location,
    status = excluded.status;

insert into toir.maintenance_requests (id, number, equipment_id, description, priority, status, created_by, created_at)
values
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'REQ-2026-001', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'Повышенная вибрация шпинделя.', 'High', 'Registered', '11111111-1111-1111-1111-111111111111', now() - interval '3 days'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'REQ-2026-002', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'Падение давления в магистрали.', 'Critical', 'Assigned', '11111111-1111-1111-1111-111111111111', now() - interval '2 days'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3', 'REQ-2026-003', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', 'Плановая диагностика после простоя.', 'Normal', 'Closed', '33333333-3333-3333-3333-333333333333', now() - interval '10 days')
on conflict (number) do update
set equipment_id = excluded.equipment_id,
    description = excluded.description,
    priority = excluded.priority,
    status = excluded.status,
    created_by = excluded.created_by;

insert into toir.work_orders (id, request_id, engineer_id, status, result, assigned_at, closed_at)
values
    ('cccccccc-cccc-cccc-cccc-ccccccccccc1', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', '22222222-2222-2222-2222-222222222222', 'InProgress', null, now() - interval '1 day', null),
    ('cccccccc-cccc-cccc-cccc-ccccccccccc2', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3', '22222222-2222-2222-2222-222222222222', 'Closed', 'Диагностика выполнена, отклонений нет.', now() - interval '9 days', now() - interval '8 days')
on conflict (id) do update
set request_id = excluded.request_id,
    engineer_id = excluded.engineer_id,
    status = excluded.status,
    result = excluded.result,
    closed_at = excluded.closed_at;

insert into toir.spare_parts (id, article, name, stock_quantity)
values
    ('dddddddd-dddd-dddd-dddd-ddddddddddd1', 'BRG-204', 'Подшипник 204', 12.00),
    ('dddddddd-dddd-dddd-dddd-ddddddddddd2', 'FLT-010', 'Фильтр воздушный', 8.00),
    ('dddddddd-dddd-dddd-dddd-ddddddddddd3', 'SL-100', 'Уплотнение вала', 5.00)
on conflict (article) do update
set name = excluded.name,
    stock_quantity = excluded.stock_quantity;

insert into audit.audit_log (id, occurred_at, user_id, role_name, action, entity_type, entity_id, result, trace_id, details)
values
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1', now() - interval '3 days', '11111111-1111-1111-1111-111111111111', 'Dispatcher', 'CREATE_REQUEST', 'maintenance_request', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'SUCCESS', 'trace-seed-001', '{"number":"REQ-2026-001"}'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2', now() - interval '2 days', '11111111-1111-1111-1111-111111111111', 'Dispatcher', 'ASSIGN_ENGINEER', 'work_order', 'cccccccc-cccc-cccc-cccc-ccccccccccc1', 'SUCCESS', 'trace-seed-002', '{"engineer":"engineer"}'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3', now() - interval '1 day', null, 'Unknown', 'ACCESS_DENIED', 'maintenance_request', null, 'DENIED', 'trace-seed-003', '{"reason":"insufficient_privileges"}')
on conflict (id) do nothing;
