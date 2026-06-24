# ════════════════════════════════════════════════════════════════════════════════
# PARTNER.IMPORT.WORKER — LOCAL BOOTSTRAP SCRIPT (PowerShell)
# ════════════════════════════════════════════════════════════════════════════════
#
# Propósito: Automatizar a execução local do Partner.Import.Worker
# Responsabilidades:
#   1. Validate .env file
#   2. Load environment variables
#   3. Display configuration summary
#   4. Build worker
#   5. Run worker
#
# Uso: .\start_partner_worker.ps1
#
# IMPORTANTE: Se receber erro de execução, execute:
#   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
#
# DICA: No PowerShell, comandos no diretório atual exigem .\
# ════════════════════════════════════════════════════════════════════════════════

param(
    [switch]$SkipBuild = $false,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[✓] $Text" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Text)
    Write-Host "[❌ ERROR] $Text" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Text)
    Write-Host "[⚠️  WARNING] $Text" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Text)
    Write-Host "[*] $Text" -ForegroundColor Cyan
}

function Get-EnvValueOrDefault {
    param(
        [hashtable]$Map,
        [string]$Key,
        [string]$Default
    )

    if ($Map.ContainsKey($Key) -and -not [string]::IsNullOrWhiteSpace($Map[$Key])) {
        return $Map[$Key]
    }

    return $Default
}

Clear-Host
Write-Host ""
Write-Host "✓ Partner.Import.Worker — Local Bootstrap" -ForegroundColor Green
Write-Host ""

Write-Header "STEP 1: Checking .env file"

if (-not (Test-Path ".env")) {
    Write-Warning-Custom ".env file not found!"
    Write-Host ""

    if (Test-Path ".env.example") {
        Write-Info "Creating .env from .env.example..."
        Copy-Item ".env.example" ".env"
        Write-Success ".env created successfully!"
        Write-Host ""
        Write-Host "⚠️  IMPORTANT: Edit .env with your environment variables before running" -ForegroundColor Yellow
        Write-Host ""
        Read-Host "Press Enter to continue..."
    } else {
        Write-Error-Custom ".env.example not found!"
        exit 1
    }
}

Write-Success ".env file exists"

Write-Header "STEP 2: Loading environment variables"

Write-Info "Parsing .env file..."

$envVars = @{}
$envContent = Get-Content ".env" -ErrorAction SilentlyContinue

foreach ($line in $envContent) {
    $line = $line.Trim()

    if ($line.StartsWith("#") -or $line -eq "") {
        continue
    }

    $parts = $line -split '=', 2
    if ($parts.Count -eq 2) {
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()

        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $envVars[$key] = $value
        [Environment]::SetEnvironmentVariable($key, $value, "Process")
    }
}

Write-Success "Environment variables loaded"
Write-Host "  Found $($envVars.Count) variables" -ForegroundColor Gray

Write-Header "STEP 3: Validating configuration"

$validationFailed = $false

if (-not $envVars.ContainsKey("ConnectionStrings__PartnerDb") -or [string]::IsNullOrWhiteSpace($envVars["ConnectionStrings__PartnerDb"])) {
    Write-Error-Custom "ConnectionStrings__PartnerDb not set in .env"
    $validationFailed = $true
}

if (-not $envVars.ContainsKey("RabbitMq__Host") -or [string]::IsNullOrWhiteSpace($envVars["RabbitMq__Host"])) {
    Write-Warning-Custom "RabbitMq__Host not set, using default: localhost"
    $envVars["RabbitMq__Host"] = "localhost"
    [Environment]::SetEnvironmentVariable("RabbitMq__Host", "localhost", "Process")
}

if ($validationFailed) {
    Write-Host ""
    Write-Error-Custom "VALIDATION FAILED"
    Write-Host "Please configure all required variables in .env file" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Success "Configuration validation passed"

Write-Header "CONFIGURATION SUMMARY"

Write-Host ""
Write-Host "🗄️  DATABASE" -ForegroundColor Cyan
Write-Host "  SQL Server: $($envVars['ConnectionStrings__PartnerDb'])"

Write-Host ""
Write-Host "🐰 RABBITMQ" -ForegroundColor Cyan
Write-Host "  Host:             $($envVars['RabbitMq__Host'])"
Write-Host ("  Port:             {0}" -f (Get-EnvValueOrDefault $envVars 'RabbitMq__Port' '5672'))
Write-Host ("  VirtualHost:      {0}" -f (Get-EnvValueOrDefault $envVars 'RabbitMq__VirtualHost' 'partner'))
Write-Host "  Username:         $($envVars['RabbitMq__Username'])"
Write-Host ("  Import Queue:     {0}" -f (Get-EnvValueOrDefault $envVars 'RabbitMq__ImportJobsQueue' 'partner.import.jobs'))

Write-Host ""
Write-Host "📊 LOGGING" -ForegroundColor Cyan
Write-Host ("  Min Level:        {0}" -f (Get-EnvValueOrDefault $envVars 'Serilog__MinimumLevel__Default' 'Information'))
Write-Host ("  Seq Enabled:      {0}" -f (Get-EnvValueOrDefault $envVars 'Serilog__Seq__Enabled' 'false'))

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Header "STEP 4: Checking .NET SDK"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Error-Custom "dotnet CLI not found"
    Write-Host "Please install .NET SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

$dotnetVersion = & dotnet --version
Write-Success "dotnet $dotnetVersion found"

if ($SkipBuild) {
    Write-Header "SKIPPING BUILD"
    Write-Warning-Custom 'Build skipped (use -SkipBuild flag)'
} else {
    Write-Header "STEP 5: Building worker"
    Write-Info "Building Partner.Import.Worker..."
    Write-Host ""

    if ($DryRun) {
        Write-Warning-Custom "DRY RUN: Skipping actual build"
    } else {
        try {
            & dotnet build Migracao\Partner.Import.Worker\Partner.Import.Worker.csproj --configuration Debug --verbosity minimal
            if ($LASTEXITCODE -ne 0) {
                Write-Host ""
                Write-Error-Custom "BUIa```powershell
param(
    [switch]$SkipBuild,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptRoot "Migracao\Partner.Import.Worker"
$ProjectFile = Join-Path $ProjectPath "Partner.Import.Worker.csproj"

function Write-Header {
    param([string]$Text)

    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[OK] $Text" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Text)
    Write-Host "[ERROR] $Text" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Text)
    Write-Host "[WARNING] $Text" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Text)
    Write-Host "[INFO] $Text" -ForegroundColor Cyan
}

function Get-EnvValueOrDefault {
    param(
        [hashtable]$Map,
        [string]$Key,
        [string]$Default
    )

    if ($Map.ContainsKey($Key) -and -not [string]::IsNullOrWhiteSpace($Map[$Key])) {
        return $Map[$Key]
    }

    return $Default
}

Write-Host ""
Write-Host "Partner.Import.Worker - Local Bootstrap" -ForegroundColor Green
Write-Host ""

Write-Header "STEP 1 - Checking .env"

$envFile = Join-Path $ScriptRoot ".env"
$envExample = Join-Path $ScriptRoot ".env.example"

if (-not (Test-Path $envFile)) {

    Write-Warning-Custom ".env not found"

    if (Test-Path $envExample) {

        Copy-Item $envExample $envFile

        Write-Success ".env created from .env.example"
        Write-Warning-Custom "Review the .env file before running"

        exit 0
    }

    Write-Error-Custom ".env.example not found"
    exit 1
}

Write-Success ".env found"

Write-Header "STEP 2 - Loading variables"

$envVars = @{}

foreach ($line in Get-Content $envFile) {

    $line = $line.Trim()

    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    if ($line.StartsWith("#")) {
        continue
    }

    $parts = $line -split '=', 2

    if ($parts.Count -ne 2) {
        continue
    }

    $key = $parts[0].Trim()
    $value = $parts[1].Trim()

    if ($value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    $envVars[$key] = $value

    if (-not $DryRun) {
        [Environment]::SetEnvironmentVariable(
            $key,
            $value,
            "Process"
        )
    }
}

Write-Success "$($envVars.Count) variables loaded"

Write-Header "STEP 3 - Validation"

$validationFailed = $false

if (-not $envVars.ContainsKey("ConnectionStrings__PartnerDb")) {

    Write-Error-Custom "ConnectionStrings__PartnerDb missing"
    $validationFailed = $true
}

if (-not $envVars.ContainsKey("RabbitMq__Host")) {

    $envVars["RabbitMq__Host"] = "localhost"

    Write-Warning-Custom "RabbitMq__Host not found. Using localhost"
}

if ($validationFailed) {
    exit 1
}

Write-Success "Validation successful"

Write-Header "Configuration"

Write-Host ""
Write-Host "DATABASE" -ForegroundColor Cyan
Write-Host "Connection String: [CONFIGURED]"

Write-Host ""
Write-Host "RABBITMQ" -ForegroundColor Cyan
Write-Host "Host: $($envVars['RabbitMq__Host'])"
Write-Host ("Port: {0}" -f (Get-EnvValueOrDefault $envVars "RabbitMq__Port" "5672"))
Write-Host ("VirtualHost: {0}" -f (Get-EnvValueOrDefault $envVars "RabbitMq__VirtualHost" "partner"))

Write-Host ""
Write-Host "LOGGING" -ForegroundColor Cyan
Write-Host ("Level: {0}" -f (Get-EnvValueOrDefault $envVars "Serilog__MinimumLevel__Default" "Information"))

Write-Header "STEP 4 - Checking .NET"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

if (-not $dotnet) {
    Write-Error-Custom ".NET SDK not installed"
    exit 1
}

$version = & dotnet --version

Write-Success ".NET SDK $version found"

if (-not (Test-Path $ProjectFile)) {

    Write-Error-Custom "Project file not found"
    Write-Host $ProjectFile

    exit 1
}

if (-not $SkipBuild) {

    Write-Header "STEP 5 - Build"

    if ($DryRun) {

        Write-Warning-Custom "DryRun enabled - build skipped"
    }
    else {

        & dotnet build $ProjectFile `
            --configuration Debug `
            --verbosity minimal

        if ($LASTEXITCODE -ne 0) {

            Write-Error-Custom "Build failed"
            exit 1
        }

        Write-Success "Build completed"
    }
}
else {

    Write-Warning-Custom "Build skipped"
}

Write-Header "STEP 6 - Run"

if ($DryRun) {

    Write-Success "DryRun completed successfully"
    exit 0
}

Push-Location $ProjectPath

try {

    & dotnet run `
        --configuration Debug `
        --no-build

    if ($LASTEXITCODE -ne 0) {

        Write-Error-Custom "Worker exited with errors"
        exit 1
    }
}
catch {

    Write-Error-Custom ("Error starting worker: {0}" -f $_.Exception.Message)
    exit 1
}
finally {

    Pop-Location
}

exit 0
```
LD FAILED"
                exit 1
            }
        } catch {
            Write-Error-Custom ("Build failed: {0}" -f $_)
            exit 1
        }
    }

    Write-Host ""
    Write-Success "Build successful"
}

Write-Header "STEP 6: Starting Partner.Import.Worker"

Write-Host ""
Write-Host "[READY TO START]" -ForegroundColor Green
Write-Host "  Database:    $($envVars['ConnectionStrings__PartnerDb'])"
Write-Host "  Environment: Development"
Write-Host ""

if ($DryRun) {
    Write-Warning-Custom "DRY RUN: Skipping worker start"
    exit 0
}

Write-Info "Starting worker (press Ctrl+C to stop)..."
Write-Host ""

Push-Location Migracao\Partner.Import.Worker

try {
    & dotnet run --configuration Debug --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Error-Custom "Failed to start Partner.Import.Worker"
        exit 1
    }
} catch {
    Write-Error-Custom "Error starting worker: $_"
    exit 1
} finally {
    Pop-Location
}

exit 0

# ════════════════════════════════════════════════════════════════════════════════
# END
# ════════════════════════════════════════════════════════════════════════════════

<#
.DESCRIPTION
Partner.Import.Worker local bootstrap script for PowerShell.

.SYNOPSIS
Automates the setup and execution of Partner.Import.Worker locally.

.PARAMETER SkipBuild
Skip the build step and go directly to running the worker.

.PARAMETER DryRun
Perform validation and display configuration without actually building or running.

.EXAMPLE
.\start_partner_worker.ps1

.EXAMPLE
.\start_partner_worker.ps1 -SkipBuild

.EXAMPLE
.\start_partner_worker.ps1 -DryRun

.NOTES
Requires PowerShell 5.0+ and .NET SDK installed.
If you get an execution policy error, run:
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
#>