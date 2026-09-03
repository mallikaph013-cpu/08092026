<#
.SYNOPSIS
    Deploys ITRepairService to C:\inetpub1\ITService
.DESCRIPTION
    This script builds, publishes, and deploys the ITRepairService ASP.NET Core application
    to the production IIS target directory C:\inetpub1\ITService.
    It handles:
      - Clean build and publish of the main project
      - Building and copying migration assemblies (SQLite + SQL Server)
      - Deploying appsettings.Production.json as production config
      - Setting up the database (SQL Server or SQLite)
      - Applying proper IIS permissions
      - Creating/updating the IIS application pool and site (optional)
.NOTES
    Run from: y:\git\DX_service\ITRepairService
    Author: DevOps
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [string]$TargetPath = 'C:\inetpub1\ITService',

    [Parameter(Mandatory = $false)]
    [switch]$BackupExisting = $true,

    [Parameter(Mandatory = $false)]
    [switch]$ConfigureIIS = $false,

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName = 'ITServiceAppPool',

    [Parameter(Mandatory = $false)]
    [string]$SiteName = 'ITService',

    [Parameter(Mandatory = $false)]
    [ValidateSet('SqlServer', 'Sqlite')]
    [string]$DatabaseProvider = 'SqlServer',

    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild = $false,

    [Parameter(Mandatory = $false)]
    [switch]$RunMigrations = $true
)

# ==================================================================
# HELPER FUNCTIONS
# ==================================================================

function Write-Banner {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host ('=' * 80) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host ('=' * 80) -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-WarningMessage {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor DarkYellow
}

function Write-ErrorStop {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    exit 1
}

# ==================================================================
# MAIN SCRIPT
# ==================================================================

Write-Banner "ITRepairService Deployment Script"
Write-Host "  Target Path    : $TargetPath"
Write-Host "  Configuration  : $Configuration"
Write-Host "  DB Provider    : $DatabaseProvider"
Write-Host "  Timestamp      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# --- 1. Resolve paths ---
$ScriptDir = Split-Path -Parent $PSCommandPath
$SolutionDir = Resolve-Path "$ScriptDir\.."
$ProjectPath = "$ScriptDir\ITRepairService.csproj"
$SqliteMigrationProject = "$SolutionDir\ITRepairService.Migrations.Sqlite\ITRepairService.Migrations.Sqlite.csproj"
$SqlServerMigrationProject = "$SolutionDir\ITRepairService.Migrations.SqlServer\ITRepairService.Migrations.SqlServer.csproj"

# Directory.Build.props redirects output to %LOCALAPPDATA%\DXService\
$localAppData = $env:LOCALAPPDATA
$buildOutputBase = "$localAppData\DXService"

Write-Step "Checking prerequisites"

# Check dotnet SDK
$dotnetVersion = dotnet --version 2>&1
if (-not $?) {
    Write-ErrorStop "dotnet SDK not found. Please install .NET 9.0 SDK."
}
Write-Host "  .NET SDK : $dotnetVersion"

# Check target directory
$targetExists = Test-Path $TargetPath
if (-not $targetExists) {
    Write-Step "Creating target directory: $TargetPath"
    New-Item -Path $TargetPath -ItemType Directory -Force | Out-Null
    Write-Success "Target directory created."
} else {
    Write-Host "  Target directory exists: $TargetPath"
}

# --- 2. Backup existing deployment ---
if ($BackupExisting -and $targetExists) {
    $backupDir = "$ScriptDir\backups"
    if (-not (Test-Path $backupDir)) {
        New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
    }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupPath = "$backupDir\ITService_$timestamp.zip"

    Write-Step "Backing up existing deployment to $backupPath"

    # Check if there are files to backup
    $existingFiles = Get-ChildItem -Path $TargetPath -ErrorAction SilentlyContinue
    if ($existingFiles.Count -gt 0) {
        if (Get-Command 'Compress-Archive' -ErrorAction SilentlyContinue) {
            Compress-Archive -Path "$TargetPath\*" -DestinationPath $backupPath -Force
            Write-Success "Backup created: $backupPath"
        } else {
            Write-WarningMessage "Compress-Archive not available. Skipping backup."
        }
    } else {
        Write-Host "  No files to backup."
    }
}

# --- 3. Restore NuGet packages ---
if (-not $SkipBuild) {
    Write-Step "Restoring NuGet packages"
    Push-Location $ScriptDir
    dotnet restore "$ScriptDir\ITRepairService.sln" 2>&1 | ForEach-Object { Write-Host "  $_" }
    if (-not $?) {
        Write-ErrorStop "dotnet restore failed."
    }
    Pop-Location
    Write-Success "Packages restored."
}

# --- 4. Build migration projects ---
if (-not $SkipBuild) {
    Write-Step "Building SQLite migration project"
    Push-Location $ScriptDir
    dotnet build $SqliteMigrationProject --configuration $Configuration 2>&1 | ForEach-Object { Write-Host "  $_" }
    if (-not $?) {
        Write-WarningMessage "SQLite migration project build failed (non-fatal)."
    }
    Pop-Location

    Write-Step "Building SQL Server migration project"
    Push-Location $ScriptDir
    dotnet build $SqlServerMigrationProject --configuration $Configuration 2>&1 | ForEach-Object { Write-Host "  $_" }
    if (-not $?) {
        Write-WarningMessage "SQL Server migration project build failed (non-fatal)."
    }
    Pop-Location
}

# --- 5. Publish the main project ---
if (-not $SkipBuild) {
    Write-Step "Publishing ITRepairService to $TargetPath"

    # Clean target first
    if ($targetExists) {
        Remove-Item -Path "$TargetPath\*" -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Determine publish framework and runtime
    $publishArgs = @(
        'publish', $ProjectPath,
        '--configuration', $Configuration,
        '--output', $TargetPath,
        '--no-restore'
    )

    Push-Location $ScriptDir
    dotnet $publishArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    if (-not $?) {
        Write-ErrorStop "dotnet publish failed."
    }
    Pop-Location
    Write-Success "Project published to $TargetPath"

    # --- 6. Copy migration assemblies ---
    Write-Step "Copying migration assemblies"

    # SQLite migrations
    # Directory.Build.props redirects build output to %LOCALAPPDATA%\DXService\<project>\bin\...
    $sqliteBuildDir = "$buildOutputBase\ITRepairService.Migrations.Sqlite\bin\$Configuration\net9.0"
    if (Test-Path $sqliteBuildDir) {
        Copy-Item -Path "$sqliteBuildDir\ITRepairService.Migrations.Sqlite.dll" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$sqliteBuildDir\ITRepairService.Migrations.Sqlite.pdb" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
        Write-Success "SQLite migration assemblies copied."
    } else {
        # Fallback to the conventional path (in case Directory.Build.props changed)
        $sqliteBuildDirFallback = "$SolutionDir\ITRepairService.Migrations.Sqlite\bin\$Configuration\net9.0"
        if (Test-Path $sqliteBuildDirFallback) {
            Copy-Item -Path "$sqliteBuildDirFallback\ITRepairService.Migrations.Sqlite.dll" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
            Copy-Item -Path "$sqliteBuildDirFallback\ITRepairService.Migrations.Sqlite.pdb" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
            Write-Success "SQLite migration assemblies copied (fallback path)."
        } else {
            Write-WarningMessage "SQLite migration assemblies not found at $sqliteBuildDir"
        }
    }

    # SQL Server migrations
    $sqlServerBuildDir = "$buildOutputBase\ITRepairService.Migrations.SqlServer\bin\$Configuration\net9.0"
    if (Test-Path $sqlServerBuildDir) {
        Copy-Item -Path "$sqlServerBuildDir\ITRepairService.Migrations.SqlServer.dll" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$sqlServerBuildDir\ITRepairService.Migrations.SqlServer.pdb" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
        Write-Success "SQL Server migration assemblies copied."
    } else {
        # Fallback to the conventional path (in case Directory.Build.props changed)
        $sqlServerBuildDirFallback = "$SolutionDir\ITRepairService.Migrations.SqlServer\bin\$Configuration\net9.0"
        if (Test-Path $sqlServerBuildDirFallback) {
            Copy-Item -Path "$sqlServerBuildDirFallback\ITRepairService.Migrations.SqlServer.dll" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
            Copy-Item -Path "$sqlServerBuildDirFallback\ITRepairService.Migrations.SqlServer.pdb" -Destination $TargetPath -Force -ErrorAction SilentlyContinue
            Write-Success "SQL Server migration assemblies copied (fallback path)."
        } else {
            Write-WarningMessage "SQL Server migration assemblies not found at $sqlServerBuildDir"
        }
    }

    # --- 7. Configure production appsettings ---
    Write-Step "Configuring production appsettings"

    $prodSettingsPath = "$ScriptDir\appsettings.Production.json"
    $targetSettingsPath = "$TargetPath\appsettings.json"

    if (Test-Path $prodSettingsPath) {
        $prodConfig = Get-Content $prodSettingsPath -Raw | ConvertFrom-Json

        # Override DatabaseProvider if specified via parameter
        $prodConfig.DatabaseProvider = $DatabaseProvider

        # Update SQL Server connection string with environment-aware server
        # (By default it uses what's in appsettings.Production.json)
        $prodConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $targetSettingsPath -Encoding UTF8
        Write-Success "appsettings.json configured for Production with DatabaseProvider=$DatabaseProvider"
    } else {
        # Use main settings as fallback
        $mainSettings = Get-Content "$ScriptDir\appsettings.json" -Raw
        Set-Content -Path $targetSettingsPath -Value $mainSettings -Encoding UTF8
        Write-WarningMessage "appsettings.Production.json not found. Using default appsettings.json."
    }

    # Copy appsettings.Production.json as reference
    if (Test-Path $prodSettingsPath) {
        Copy-Item -Path $prodSettingsPath -Destination "$TargetPath\appsettings.Production.json" -Force
    }
} else {
    Write-Step "Skipping build/publish (SkipBuild flag set)"
}

# --- 8. Ensure web.config for IIS with Production environment ---
Write-Step "Ensuring web.config for IIS"

$webConfigPath = "$TargetPath\web.config"
# ASP.NET Core publishes its own web.config, but it does NOT set ASPNETCORE_ENVIRONMENT.
# Always write our own so the deployed site runs in Production.
$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\ITRepairService.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
Set-Content -Path $webConfigPath -Value $webConfig -Encoding UTF8
Write-Success "web.config ensured with ASPNETCORE_ENVIRONMENT=Production."

# --- 9. Create log directory ---
$logDir = "$TargetPath\logs"
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    Write-Success "Log directory created."
}

# --- 10. Set IIS permissions ---
Write-Step "Setting IIS permissions on $TargetPath"
try {
    # Grant IIS AppPool identity / IIS_USRS read/execute access
    $acl = Get-Acl -Path $TargetPath
    $permission = "IIS_IUSRS","Read,Write,Modify","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)

    # Also grant NETWORK SERVICE for app pool identity
    $permission2 = "NETWORK SERVICE","Read,Write,Modify","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule $permission2
    $acl.SetAccessRule($accessRule2)

    Set-Acl -Path $TargetPath -AclObject $acl
    Write-Success "IIS permissions applied."
} catch {
    Write-WarningMessage "Failed to set IIS permissions: $_"
}

# --- 11. Configure IIS Application Pool and Site (optional) ---
if ($ConfigureIIS) {
    Write-Step "Configuring IIS"
    try {
        Import-Module WebAdministration -ErrorAction Stop

        # Create application pool if it doesn't exist
        $appPoolPath = "IIS:\AppPools\$AppPoolName"
        if (-not (Test-Path $appPoolPath)) {
            New-Item -Path $appPoolPath -Force | Out-Null
            Set-ItemProperty -Path $appPoolPath -Name "managedRuntimeVersion" -Value ""
            Set-ItemProperty -Path $appPoolPath -Name "enable32BitAppOnWin64" -Value $false
            Set-ItemProperty -Path $appPoolPath -Name "startMode" -Value "AlwaysRunning"
            Write-Success "IIS Application Pool '$AppPoolName' created."
        } else {
            Write-Host "  IIS Application Pool '$AppPoolName' already exists."
        }

        # Stop/Start app pool to refresh
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Start-WebAppPool -Name $AppPoolName

        # Create site if it doesn't exist
        $sitePath = "IIS:\Sites\$SiteName"
        if (-not (Test-Path $sitePath)) {
            New-Item -Path $sitePath -PhysicalPath $TargetPath -Bindings @{protocol="http";bindingInformation="*:80:itservice.local"} -Force | Out-Null
            Set-ItemProperty -Path $sitePath -Name "applicationPool" -Value $AppPoolName
            Write-Success "IIS Site '$SiteName' created."
        } else {
            Set-ItemProperty -Path $sitePath -Name "physicalPath" -Value $TargetPath
            Write-Host "  IIS Site '$SiteName' updated."
        }

        # Restart the site
        Stop-WebSite -Name $SiteName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Start-WebSite -Name $SiteName
        Write-Success "IIS site '$SiteName' restarted."

    } catch {
        Write-WarningMessage "IIS configuration failed (WebAdministration module may not be available): $_"
        Write-WarningMessage "Please configure IIS manually - create an App Pool and point the site to $TargetPath"
    }
} else {
    Write-Host "  Skipping IIS configuration (use -ConfigureIIS to enable)."
}

# --- 12. Run database migrations (optional) ---
if ($RunMigrations) {
    Write-Step "Running database migrations"

    # For SQLite: the app will auto-migrate on startup via Program.cs (db.Database.Migrate())
    # For SQL Server: the app uses EnsureCreated()

    if ($DatabaseProvider -eq 'SqlServer') {
        Write-Host "  Database Provider: SQL Server"
        Write-Host "  The application will auto-create the database on first run using EnsureCreated()."
        Write-Host "  Make sure SQL Server is accessible with the connection string configured."
    } else {
        Write-Host "  Database Provider: SQLite"
        Write-Host "  The application will auto-migrate the SQLite database on startup."
    }

    # Run the migration script that applies pending migrations
    $migrationScript = "$ScriptDir\ApplyDatabaseMigration.ps1"
    if (Test-Path $migrationScript) {
        Write-Host "  Applying database migrations via ApplyDatabaseMigration.ps1..."
        & $migrationScript -Configuration $Configuration
        if ($?) {
            Write-Success "Database migrations applied."
        } else {
            Write-WarningMessage "Database migration script exited with errors. Check logs for details."
        }
    } else {
        Write-Host "  No ApplyDatabaseMigration.ps1 found. Migrations will run on application startup."
    }
} else {
    Write-Host "  Skipping database migrations (use -RunMigrations to enable)."
}

# --- 13. Verify deployment ---
Write-Step "Verifying deployment"

$requiredFiles = @(
    'ITRepairService.dll',
    'web.config',
    'appsettings.json'
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $TargetPath $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-WarningMessage "Missing required files: $($missingFiles -join ', ')"
} else {
    Write-Success "All required files present in $TargetPath"
}

# Show deployment summary
Write-Banner "Deployment Summary"
Write-Host "  Target Path    : $TargetPath" -ForegroundColor Green
Write-Host "  Configuration  : $Configuration" -ForegroundColor Green
Write-Host "  DB Provider    : $DatabaseProvider" -ForegroundColor Green
Write-Host "  Size           : "
$totalSize = (Get-ChildItem -Path $TargetPath -Recurse | Measure-Object -Property Length -Sum).Sum
if ($totalSize -gt 1GB) {
    Write-Host ("    {0:N2} GB" -f ($totalSize / 1GB)) -ForegroundColor Green
} elseif ($totalSize -gt 1MB) {
    Write-Host ("    {0:N2} MB" -f ($totalSize / 1MB)) -ForegroundColor Green
} else {
    Write-Host ("    {0:N2} KB" -f ($totalSize / 1KB)) -ForegroundColor Green
}
Write-Host "  File Count     : $( (Get-ChildItem -Path $TargetPath -Recurse -File).Count )" -ForegroundColor Green
Write-Host "  Timestamp      : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Green
Write-Host "  Status         : DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Banner ""

Write-Host ""
Write-Host "  To deploy to IIS manually:"
Write-Host "    - Open IIS Manager"
Write-Host "    - Create Application Pool '.NET CLR Version: No Managed Code'"
Write-Host "    - Create/Update Site pointing to: $TargetPath"
Write-Host "    - Set ASPNETCORE_ENVIRONMENT to 'Production'"
Write-Host "    - Restart the site"
Write-Host ""
Write-Host "  To test the application directly (without IIS):"
Write-Host "    cd $TargetPath"
Write-Host "    dotnet ITRepairService.dll --urls http://localhost:5000"
Write-Host ""