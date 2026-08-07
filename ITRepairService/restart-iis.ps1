# Requires Administrator privileges
# This script stops IIS, deploys the application, and starts IIS again

param(
    [string]$SiteName = 'ITService',
    [string]$AppPoolName = 'ITServiceAppPool'
)

Write-Host '=== Stopping IIS ===' -ForegroundColor Yellow

try {
    Import-Module WebAdministration -ErrorAction Stop
    
    # Stop the website
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Stop-Website -Name $SiteName
        Write-Host "Website '$SiteName' stopped." -ForegroundColor Green
    } else {
        Write-Host "Website '$SiteName' not found." -ForegroundColor Yellow
    }
    
    # Stop the app pool
    if (Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Stop-WebAppPool -Name $AppPoolName
        Write-Host "App Pool '$AppPoolName' stopped." -ForegroundColor Green
    } else {
        Write-Host "App Pool '$AppPoolName' not found." -ForegroundColor Yellow
    }
    
    Start-Sleep -Seconds 3
    Write-Host 'IIS stopped successfully.' -ForegroundColor Green
} catch {
    Write-Host "Failed to stop IIS: $_" -ForegroundColor Red
    Write-Host 'Please run this script as Administrator.' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '=== Deploying Application ===' -ForegroundColor Yellow

# Run the deployment script
$deployScript = Join-Path $PSScriptRoot 'Deploy-ITService.ps1'
if (Test-Path $deployScript) {
    & $deployScript -Configuration Release -DatabaseProvider SqlServer -ConfigureIIS
    Write-Host 'Deployment completed.' -ForegroundColor Green
} else {
    Write-Host "Deploy script not found at $deployScript" -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '=== Starting IIS ===' -ForegroundColor Yellow

try {
    # Start the app pool
    Start-WebAppPool -Name $AppPoolName
    Write-Host "App Pool '$AppPoolName' started." -ForegroundColor Green
    
    # Start the website
    Start-Website -Name $SiteName
    Write-Host "Website '$SiteName' started." -ForegroundColor Green
    
    Write-Host 'IIS started successfully.' -ForegroundColor Green
} catch {
    Write-Host "Failed to start IIS: $_" -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '=== Deployment Complete ===' -ForegroundColor Cyan
Write-Host "Site: http://$SiteName" -ForegroundColor Green