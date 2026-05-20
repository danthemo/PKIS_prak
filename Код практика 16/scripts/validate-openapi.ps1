$ErrorActionPreference = "Stop"

$openApiPath = Join-Path $PSScriptRoot "..\openapi\openapi-v1.yaml"

if (-not (Test-Path $openApiPath)) {
    Write-Error "OpenAPI file not found: $openApiPath"
    exit 1
}

Write-Host "OpenAPI file found: $openApiPath"
exit 0

