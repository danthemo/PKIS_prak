-- Active connections by state.
select state, count(*) as connection_count
from pg_stat_activity
group by state
order by connection_count desc;

-- Long-running queries.
select pid, usename, datname, state, now() - query_start as duration, query
from pg_stat_activity
where query_start is not null
  and now() - query_start > interval '1 minute'
order by duration desc;

-- Lock waits.
select pid, usename, datname, wait_event_type, wait_event, state, query
from pg_stat_activity
where wait_event_type = 'Lock';

-- Database size.
select datname, pg_size_pretty(pg_database_size(datname)) as database_size
from pg_database
where datname = current_database();

-- Table sizes.
select schemaname, relname, pg_size_pretty(pg_total_relation_size(relid)) as total_size
from pg_catalog.pg_statio_user_tables
where schemaname in ('toir', 'audit')
order by pg_total_relation_size(relid) desc;

-- Recent audit errors and denied operations.
select occurred_at, role_name, action, entity_type, entity_id, result, trace_id, details
from audit.audit_log
where result <> 'SUCCESS'
order by occurred_at desc
limit 20;

-- Request count by status.
select status, count(*) as request_count
from toir.maintenance_requests
group by status
order by status;

-- Age of the latest audit event.
select now() - max(occurred_at) as latest_audit_event_age
from audit.audit_log;

-- pg_stat_activity details.
select pid, usename, application_name, client_addr, state, backend_start, query_start, wait_event_type, wait_event, query
from pg_stat_activity
where datname = current_database()
order by query_start nulls last;

-- pg_locks details.
select l.locktype, l.mode, l.granted, l.pid, a.usename, a.query
from pg_locks l
left join pg_stat_activity a on a.pid = l.pid
where a.datname = current_database() or a.datname is null
order by l.granted, l.locktype, l.mode;
