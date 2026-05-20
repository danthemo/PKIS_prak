do $$
begin
    if not exists (select 1 from pg_roles where rolname = 'toir_app') then
        create role toir_app login password 'change_app_password';
    end if;

    if not exists (select 1 from pg_roles where rolname = 'toir_analyst') then
        create role toir_analyst login password 'change_analyst_password';
    end if;

    if not exists (select 1 from pg_roles where rolname = 'toir_auditor') then
        create role toir_auditor login password 'change_auditor_password';
    end if;

    if not exists (select 1 from pg_roles where rolname = 'toir_backup') then
        create role toir_backup login password 'change_backup_password';
    end if;
end
$$;

comment on role toir_app is 'Application role. Replace placeholder password with a secret in real environments.';
comment on role toir_analyst is 'Read-only role for operational analytics. Replace placeholder password with a secret in real environments.';
comment on role toir_auditor is 'Read-only role for audit data. Replace placeholder password with a secret in real environments.';
comment on role toir_backup is 'Role for logical backups. Replace placeholder password with a secret in real environments.';
