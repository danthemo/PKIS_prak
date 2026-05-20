create index if not exists ix_requests_status_created_at
    on toir.maintenance_requests(status, created_at desc);

create index if not exists ix_requests_equipment_created_at
    on toir.maintenance_requests(equipment_id, created_at desc);

create index if not exists ix_work_orders_engineer_status
    on toir.work_orders(engineer_id, status, assigned_at desc);

create index if not exists ix_work_orders_status_closed_at
    on toir.work_orders(status, closed_at desc);

create index if not exists ix_audit_log_trace_id
    on audit.audit_log(trace_id);

create index if not exists ix_audit_log_occurred_at
    on audit.audit_log(occurred_at);

create index if not exists ix_audit_log_entity
    on audit.audit_log(entity_type, entity_id);
