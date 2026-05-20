create or replace function audit.write_event(
    p_user_id uuid,
    p_role_name varchar,
    p_action varchar,
    p_entity_type varchar,
    p_entity_id uuid,
    p_result varchar,
    p_trace_id varchar,
    p_details jsonb default null
)
returns uuid
language plpgsql
security definer
set search_path = audit, pg_temp
as $$
declare
    v_id uuid := gen_random_uuid();
begin
    insert into audit.audit_log (
        id,
        user_id,
        role_name,
        action,
        entity_type,
        entity_id,
        result,
        trace_id,
        details
    )
    values (
        v_id,
        p_user_id,
        p_role_name,
        p_action,
        p_entity_type,
        p_entity_id,
        p_result,
        p_trace_id,
        p_details
    );

    return v_id;
end;
$$;

grant execute on function audit.write_event(uuid, varchar, varchar, varchar, uuid, varchar, varchar, jsonb) to toir_app;

select audit.write_event('11111111-1111-1111-1111-111111111111', 'Dispatcher', 'CREATE_REQUEST', 'maintenance_request', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'SUCCESS', 'trace-example-create-request', '{"source":"sql_example"}');
select audit.write_event('33333333-3333-3333-3333-333333333333', 'Chief', 'ASSIGN_ENGINEER', 'work_order', 'cccccccc-cccc-cccc-cccc-ccccccccccc1', 'SUCCESS', 'trace-example-assign-engineer', '{"engineer_id":"22222222-2222-2222-2222-222222222222"}');
select audit.write_event('22222222-2222-2222-2222-222222222222', 'Engineer', 'CHANGE_REQUEST_STATUS', 'maintenance_request', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'SUCCESS', 'trace-example-change-status', '{"from":"Assigned","to":"InProgress"}');
select audit.write_event('22222222-2222-2222-2222-222222222222', 'Engineer', 'CLOSE_WORK_ORDER', 'work_order', 'cccccccc-cccc-cccc-cccc-ccccccccccc2', 'SUCCESS', 'trace-example-close-work-order', '{"result":"completed"}');
select audit.write_event(null, 'Analyst', 'ACCESS_DENIED', 'audit_log', null, 'DENIED', 'trace-example-access-denied', '{"reason":"audit_schema_is_restricted"}');
