param(
    [Parameter(Mandatory = $true)]
    [string]$DumpPath,
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$SourceDatabase = "toir_db",
    [string]$RestoreDatabase = "toir_restore_check",
    [string]$User = "toir_admin"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $DumpPath)) {
    throw "Dump file was not found: $DumpPath"
}

$resolvedDump = Resolve-Path $DumpPath
$dumpFileName = Split-Path $resolvedDump -Leaf

Write-Host "Restoring '$resolvedDump' into separate database '$RestoreDatabase'."
Write-Host "The main database '$SourceDatabase' is not modified."

$psql = Get-Command psql -ErrorAction SilentlyContinue
$pgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue

if ($psql -and $pgRestore) {
    if (-not $env:PGPASSWORD) {
        Write-Host "PGPASSWORD is not set. Set it before running the script if password authentication is required."
    }

    $exists = ((& psql -h $HostName -p $Port -U $User -d postgres -tAc "select 1 from pg_database where datname = '$RestoreDatabase'") | Out-String).Trim()
    if ($exists -ne "1") {
        & psql -h $HostName -p $Port -U $User -d postgres -v ON_ERROR_STOP=1 -c "create database $RestoreDatabase"
    }
    & pg_restore -h $HostName -p $Port -U $User -d $RestoreDatabase --clean --if-exists $resolvedDump
} else {
    Write-Host "Local psql/pg_restore were not found. Using PostgreSQL tools inside Docker container."
    $containerDump = "/backups/$dumpFileName"
    $exists = ((docker compose exec -T postgres psql -U $User -d postgres -tAc "select 1 from pg_database where datname = '$RestoreDatabase'") | Out-String).Trim()
    if ($exists -ne "1") {
        docker compose exec -T postgres psql -U $User -d postgres -v ON_ERROR_STOP=1 -c "create database $RestoreDatabase"
    }
    docker compose exec -T postgres pg_restore -U $User -d $RestoreDatabase --clean --if-exists $containerDump
}

$checks = @"
select 'equipment_count' as check_name, count(*)::text as check_value from toir.equipment
union all
select 'maintenance_requests_count', count(*)::text from toir.maintenance_requests
union all
select 'work_orders_count', count(*)::text from toir.work_orders
union all
select 'work_orders_without_request', count(*)::text
from toir.work_orders wo
left join toir.maintenance_requests mr on mr.id = wo.request_id
where mr.id is null
union all
select 'requests_without_equipment', count(*)::text
from toir.maintenance_requests mr
left join toir.equipment e on e.id = mr.equipment_id
where e.id is null;
"@

Write-Host "Running restore checks..."

if ($psql) {
    & psql -h $HostName -p $Port -U $User -d $RestoreDatabase -v ON_ERROR_STOP=1 -c $checks
} else {
    docker compose exec -T postgres psql -U $User -d $RestoreDatabase -v ON_ERROR_STOP=1 -c $checks
}
