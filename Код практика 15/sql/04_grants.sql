grant connect on database toir_db to toir_app, toir_analyst, toir_auditor, toir_backup;

grant usage on schema toir, audit to toir_app;
grant select, insert, update on all tables in schema toir to toir_app;
grant insert on all tables in schema audit to toir_app;

grant usage on schema toir to toir_analyst;
grant select on all tables in schema toir to toir_analyst;

grant usage on schema audit to toir_auditor;
grant select on all tables in schema audit to toir_auditor;

grant usage on schema toir, audit to toir_backup;
grant select on all tables in schema toir to toir_backup;
grant select on all tables in schema audit to toir_backup;

alter default privileges in schema toir grant select, insert, update on tables to toir_app;
alter default privileges in schema audit grant insert on tables to toir_app;
alter default privileges in schema toir grant select on tables to toir_analyst;
alter default privileges in schema audit grant select on tables to toir_auditor;
alter default privileges in schema toir grant select on tables to toir_backup;
alter default privileges in schema audit grant select on tables to toir_backup;
