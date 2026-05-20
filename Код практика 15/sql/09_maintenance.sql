analyze toir.maintenance_requests;
analyze toir.work_orders;

vacuum analyze toir.maintenance_requests;
vacuum analyze toir.work_orders;

do $$
begin
    if to_regclass('toir.ix_requests_status_created_at') is not null then
        reindex index toir.ix_requests_status_created_at;
    end if;
end
$$;

-- Dead tuple check.
select schemaname, relname, n_live_tup, n_dead_tup, last_vacuum, last_autovacuum, last_analyze, last_autoanalyze
from pg_stat_user_tables
where schemaname in ('toir', 'audit')
order by n_dead_tup desc;

-- Index size estimate.
select
    schemaname,
    relname as table_name,
    indexrelname as index_name,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    idx_scan
from pg_stat_user_indexes
where schemaname in ('toir', 'audit')
order by pg_relation_size(indexrelid) desc;
