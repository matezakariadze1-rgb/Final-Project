$ErrorActionPreference = 'Stop'
$spaceHubRoot = Split-Path -Parent $PSScriptRoot
$settingsPath = Join-Path $spaceHubRoot 'src/SpaceHub.Api/appsettings.Development.json'
$settings = if (Test-Path -LiteralPath $settingsPath) { Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }
if (-not $settings.PSObject.Properties['Jwt']) { $settings | Add-Member -NotePropertyName Jwt -NotePropertyValue ([pscustomobject]@{}) }
if ([string]::IsNullOrWhiteSpace($settings.Jwt.Key)) {
    $bytes = New-Object byte[] 48
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $settings.Jwt | Add-Member -NotePropertyName Key -NotePropertyValue ([Convert]::ToBase64String($bytes)) -Force
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $settingsPath -Encoding utf8
    Write-Output 'Local JWT key initialized in ignored development settings.'
} else { Write-Output 'Existing development JWT key preserved.' }
