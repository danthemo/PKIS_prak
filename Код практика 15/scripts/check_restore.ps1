param(
    [string]$BackupDir = "backups",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$SourceDatabase = "toir_db",
    [string]$RestoreDatabase = "toir_restore_check",
    [string]$User = "toir_admin"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupDir)) {
    throw "Backup directory was not found: $BackupDir"
}

$latestDump = Get-ChildItem -Path $BackupDir -Filter "*.dump" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latestDump) {
    throw "No .dump files were found in $BackupDir"
}

Write-Host "Latest backup: $($latestDump.FullName)"

& "$PSScriptRoot\restore.ps1" `
    -DumpPath $latestDump.FullName `
    -HostName $HostName `
    -Port $Port `
    -SourceDatabase $SourceDatabase `
    -RestoreDatabase $RestoreDatabase `
    -User $User
