$target = 'C:\inetpub1\IT_Service'

Write-Host '=== Deployed Files in C:\inetpub1\IT_Service ==='
Get-ChildItem $target | Select-Object Name, Length | Format-Table -AutoSize

Write-Host ''
Write-Host '=== Required Files Check ==='
$required = @('ITRepairService.dll', 'web.config', 'appsettings.json', 'ITRepairService.Migrations.Sqlite.dll', 'ITRepairService.Migrations.SqlServer.dll')
foreach ($f in $required) {
    $p = Join-Path $target $f
    if (Test-Path $p) {
        Write-Host "[OK] $f"
    } else {
        Write-Host "[MISSING] $f"
    }
}

# Check LDAP config in appsettings.json
$appSettingsPath = Join-Path $target 'appsettings.json'
if (Test-Path $appSettingsPath) {
    $config = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    if ($config.LDAP) {
        Write-Host "[OK] LDAP section found in appsettings.json"
        Write-Host "    Server: $($config.LDAP.Server)"
    } else {
        Write-Host "[MISSING] LDAP section NOT found in appsettings.json"
    }
    if ($config.SeedAdmin) {
        Write-Host "[OK] SeedAdmin section found in appsettings.json"
    } else {
        Write-Host "[MISSING] SeedAdmin section NOT found in appsettings.json"
    }
}

# Check ASPNETCORE_ENVIRONMENT in web.config
$webConfigPath = Join-Path $target 'web.config'
if (Test-Path $webConfigPath) {
    $webConfig = Get-Content $webConfigPath -Raw
    if ($webConfig -match 'ASPNETCORE_ENVIRONMENT.*Production') {
        Write-Host "[OK] ASPNETCORE_ENVIRONMENT=Production in web.config"
    } else {
        Write-Host "[MISSING] ASPNETCORE_ENVIRONMENT not set to Production in web.config"
    }
}

Write-Host ''
Write-Host '=== Total Size ==='
$totalSize = (Get-ChildItem $target -Recurse | Measure-Object -Property Length -Sum).Sum
Write-Host ('Total: {0:N2} MB' -f ($totalSize / 1MB))
Write-Host ('Files: {0}' -f (Get-ChildItem $target -Recurse -File).Count)