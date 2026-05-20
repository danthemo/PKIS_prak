create table if not exists toir.users (
    id uuid primary key default gen_random_uuid(),
    login varchar(100) not null unique,
    full_name varchar(200) not null,
    role_name varchar(64) not null,
    created_at timestamp not null default now()
);

create table if not exists toir.equipment (
    id uuid primary key default gen_random_uuid(),
    inventory_number varchar(64) not null unique,
    name varchar(200) not null,
    location varchar(200) not null,
    status varchar(32) not null check (status in ('Active', 'Inactive', 'Decommissioned'))
);

create table if not exists toir.maintenance_requests (
    id uuid primary key default gen_random_uuid(),
    number varchar(32) not null unique,
    equipment_id uuid not null references toir.equipment(id),
    description text not null,
    priority varchar(32) not null check (priority in ('Low', 'Normal', 'High', 'Critical')),
    status varchar(32) not null check (status in ('Registered', 'Assigned', 'InProgress', 'WaitingParts', 'Done', 'Closed', 'Cancelled')),
    created_by uuid not null references toir.users(id),
    created_at timestamp not null default now(),
    version integer not null default 1
);

create table if not exists toir.work_orders (
    id uuid primary key default gen_random_uuid(),
    request_id uuid not null references toir.maintenance_requests(id),
    engineer_id uuid not null references toir.users(id),
    status varchar(32) not null check (status in ('Assigned', 'InProgress', 'Completed', 'Closed', 'Cancelled')),
    result text null,
    assigned_at timestamp not null default now(),
    closed_at timestamp null,
    version integer not null default 1
);

create table if not exists toir.spare_parts (
    id uuid primary key default gen_random_uuid(),
    article varchar(64) not null unique,
    name varchar(200) not null,
    stock_quantity numeric(12,2) not null check (stock_quantity >= 0)
);

create table if not exists toir.spare_part_usage (
    id uuid primary key default gen_random_uuid(),
    work_order_id uuid not null references toir.work_orders(id),
    spare_part_id uuid not null references toir.spare_parts(id),
    quantity numeric(12,2) not null check (quantity > 0),
    used_at timestamp not null default now()
);

create table if not exists audit.audit_log (
    id uuid primary key default gen_random_uuid(),
    occurred_at timestamp not null default now(),
    user_id uuid null,
    role_name varchar(64) not null,
    action varchar(128) not null,
    entity_type varchar(128) not null,
    entity_id uuid null,
    result varchar(64) not null,
    trace_id varchar(128) not null,
    details jsonb null
);
