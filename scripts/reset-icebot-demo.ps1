[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

if ($env:ASPNETCORE_ENVIRONMENT -and $env:ASPNETCORE_ENVIRONMENT -ne "Development") {
    throw "This reset is Development-only. Remove ASPNETCORE_ENVIRONMENT or set it to Development."
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
Push-Location $projectRoot
try {
    dotnet run --project .\src\WebAPI\WebAPI.csproj -- --reset-icebot-demo
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
