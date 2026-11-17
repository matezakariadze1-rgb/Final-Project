$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiRoot = Join-Path $projectRoot 'src/SpaceHub.Api'
$artifacts = Join-Path $projectRoot '.artifacts/demo-seed'
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Push-Location $apiRoot
    try {
        # Separate build outputs avoid DLL locks held by the running website.
        dotnet build SpaceHub.Api.csproj --artifacts-path $artifacts -m:1 -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) {
            throw 'Demo build failed. See the compiler/NuGet errors above; no import was attempted.'
        }
        $seedDll = Join-Path $artifacts 'bin/SpaceHub.Api/debug/SpaceHub.Api.dll'
        if (-not (Test-Path -LiteralPath $seedDll)) {
            throw "Demo executable was not produced: $seedDll"
        }
        # Keep API as working directory so development settings are loaded.
        dotnet $seedDll --seed-demo
        if ($LASTEXITCODE -ne 0) {
            throw 'Demo import failed after a successful build. See the database error above.'
        }
    } finally { Pop-Location }
} finally { $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment }
