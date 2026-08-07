@echo off
REM ===================================================
REM  Deploy ITRepairService to C:\inetpub1\ITService
REM ===================================================
title Deploying ITRepairService...

echo ===================================================
echo  ITRepairService Deployment
echo ===================================================
echo.
echo This will build and deploy to C:\inetpub1\ITService
echo.

REM Change to script directory
cd /d "%~dp0"

REM Run the PowerShell deployment script
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-ITService.ps1" %*

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