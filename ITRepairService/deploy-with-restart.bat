@echo off
REM ===================================================
REM  Deploy with IIS Restart (requires Administrator)
REM ===================================================
title Deploying ITRepairService with IIS Restart...

echo ===================================================
echo  ITRepairService Deployment with IIS Restart
echo ===================================================
echo.
echo This will:
echo   1. Stop IIS
echo   2. Deploy the application
echo   3. Start IIS again
echo.
echo NOTE: This script must be run as Administrator
echo ===================================================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] This script requires Administrator privileges!
    echo Please right-click and select "Run as Administrator"
    pause
    exit /b 1
)

REM Change to script directory
cd /d "%~dp0"

REM Run the PowerShell deployment script with IIS restart
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0restart-iis.ps1"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Deployment failed with exit code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Deployment completed successfully.
echo.
pause