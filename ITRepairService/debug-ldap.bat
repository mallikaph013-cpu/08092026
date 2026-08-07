@echo off
echo ===================================================
echo  Debug LDAP Connection from IIS
echo ===================================================
echo.

echo This will test LDAP connection from the server
echo Make sure the application is running in IIS
echo.

echo Checking IIS Application Pool Identity...
echo.

echo Current App Pool Identity:
powershell -NoProfile -Command "Import-Module WebAdministration; Get-ItemProperty IIS:\AppPools\ITServiceAppPool -Name processModel.identityType | Select-Object -ExpandProperty processModel.identityType"

echo.
echo ===================================================
echo  Testing LDAP Connection
echo ===================================================
echo.

echo Testing connection to ASIVM1001AD on port 389...
powershell -NoProfile -Command "try { $tcp = New-Object System.Net.Sockets.TcpClient; $tcp.Connect('ASIVM1001AD', 389); Write-Host '[OK] Port 389 is reachable' -ForegroundColor Green; $tcp.Close() } catch { Write-Host '[FAIL] Cannot connect to ASIVM1001AD:389' -ForegroundColor Red; Write-Host $_.Exception.Message }"

echo.
echo ===================================================
echo  Checking Windows Event Logs
echo ===================================================
echo.
echo Recent Application Errors:
powershell -NoProfile -Command "Get-EventLog -LogName Application -EntryType Error -Newest 10 -After '1 hour ago' -ErrorAction SilentlyContinue | Select-Object TimeGenerated, Source, Message | Format-List"

echo.
echo ===================================================
echo  Checking IIS Logs
echo ===================================================
echo.
echo Recent IIS Requests:
powershell -NoProfile -Command "Get-Content 'C:\inetpub1\IT_Service\logs\*' -ErrorAction SilentlyContinue | Select-Object -Last 20"

echo.
echo ===================================================
echo  Debug Information
echo ===================================================
echo.
echo Please check:
echo 1. IIS App Pool Identity (should be ApplicationPoolIdentity or NetworkService)
echo 2. Firewall allows outbound port 389 to ASIVM1001AD
echo 3. AD Server (ASIVM1001AD) is accessible from this server
echo 4. LDAP settings in appsettings.json are correct
echo.
pause