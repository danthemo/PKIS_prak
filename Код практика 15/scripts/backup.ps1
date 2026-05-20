param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "toir_db",
    [string]$User = "toir_admin",
    [string]$OutputDir = "backups"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$fileName = "$Database`_$timestamp.dump"
$outputPath = Join-Path $OutputDir $fileName

Write-Host "Creating backup for database '$Database'..."

$pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue
if ($pgDump) {
    if (-not $env:PGPASSWORD) {
        Write-Host "PGPASSWORD is not set. Set it before running the script if password authentication is required."
    }

    & pg_dump -h $HostName -p $Port -U $User -d $Database -F c -f $outputPath
} else {
    Write-Host "Local pg_dump was not found. Using pg_dump inside Docker container."
    $containerPath = "/backups/$fileName"
    docker compose exec -T postgres pg_dump -U $User -d $Database -F c -f $containerPath
}

if (-not (Test-Path $outputPath)) {
    throw "Backup file was not created: $outputPath"
}

$file = Get-Item $outputPath
if ($file.Length -le 0) {
    throw "Backup file is empty: $outputPath"
}

Write-Host "Backup created: $($file.FullName)"
Write-Host "Backup size: $($file.Length) bytes"
