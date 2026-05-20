create extension if not exists pgcrypto;

create schema if not exists toir;
create schema if not exists audit;

comment on schema toir is 'Operational schema for maintenance and repair data.';
comment on schema audit is 'Audit events for database and application actions.';
