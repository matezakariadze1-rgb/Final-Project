$ErrorActionPreference = 'Stop'
if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
    throw 'SQL Server LocalDB is not installed or sqllocaldb is not on PATH. Install SQL Server Express LocalDB using Visual Studio Installer.'
}
$instances = & sqllocaldb info 2>&1
$info = $instances
if ($LASTEXITCODE -ne 0 -or -not ($instances | Where-Object { $_.ToString().Trim() -eq 'MSSQLLocalDB' })) {
    Write-Host ($info -join [Environment]::NewLine)
    throw 'Cannot inspect MSSQLLocalDB. Run this script in a normal Windows PowerShell terminal under your Windows account. Restricted environments may deny LocalDB registry access. No database was deleted or reset.'
}
$start = & sqllocaldb start MSSQLLocalDB 2>&1
if ($LASTEXITCODE -ne 0 -or ($start -match 'failed|error')) {
    Write-Host ($start -join [Environment]::NewLine)
    throw 'MSSQLLocalDB could not start. Inspect the LocalDB error log before changing or reinstalling the instance.'
}
Write-Host 'MSSQLLocalDB is available.' -ForegroundColor Green
