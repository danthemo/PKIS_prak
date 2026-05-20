-- 1. Update with optimistic locking.
update maintenance_requests
set status = @new_status,
    version = version + 1
where id = @request_id
  and version = @expected_version;

-- 2. Conditional update for spare part reservation.
update spare_parts
set stock_quantity = stock_quantity - @quantity,
    version = version + 1
where id = @spare_part_id
  and stock_quantity >= @quantity
returning id, stock_quantity, version;

-- 3. Idempotency key upsert.
insert into idempotency_keys(operation_id, operation_name, result_json, created_at)
values (@operation_id, @operation_name, @result_json, now())
on conflict (operation_id) do nothing;

-- 4. CTE for request status recalculation.
with active_orders as (
    select count(*) as active_count
    from work_orders
    where request_id = @request_id
      and status not in ('Closed', 'Cancelled')
),
updated_request as (
    update maintenance_requests
    set status = 'Done',
        version = version + 1
    where id = @request_id
      and (select active_count from active_orders) = 0
      and status = 'InProgress'
    returning id, status, version
)
select * from updated_request;

-- 5. PostgreSQL lock diagnostics.
select pid, state, wait_event_type, wait_event, query
from pg_stat_activity
where wait_event_type is not null;

-- 6. Waiting locks.
select locktype, relation::regclass, mode, granted, pid
from pg_locks
where not granted;
