$ErrorActionPreference = "Stop"

$migrationsPath = Join-Path $PSScriptRoot "..\migrations"
$namePattern = "^\d{8}_\d{3}_[a-z0-9_]+\.sql$"

if (-not (Test-Path $migrationsPath)) {
    Write-Error "Migrations directory not found: $migrationsPath"
    exit 1
}

$migrationFiles = Get-ChildItem -Path $migrationsPath -Filter "*.sql" | Sort-Object Name

if ($migrationFiles.Count -eq 0) {
    Write-Error "No migration files found in: $migrationsPath"
    exit 1
}

foreach ($file in $migrationFiles) {
    Write-Host $file.Name
    if ($file.Name -notmatch $namePattern) {
        Write-Error "Invalid migration file name: $($file.Name)"
        exit 1
    }
}

Write-Host "Migration files check completed successfully."
exit 0

