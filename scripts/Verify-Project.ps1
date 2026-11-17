$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    & (Join-Path $PSScriptRoot 'Test-LocalDb.ps1')
    & (Join-Path $PSScriptRoot 'Build-Frontend.ps1')
    dotnet test .\SpaceHub.slnx -m:1 -p:UseSharedCompilation=false --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw 'SpaceHub tests failed. Read the test output above.' }
    Write-Host 'Frontend build and SpaceHub tests passed.' -ForegroundColor Green
} finally { Pop-Location }
