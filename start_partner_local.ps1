# ════════════════════════════════════════════════════════════════════════════════
# PARTNER.MODERN — LOCAL BOOTSTRAP SCRIPT (PowerShell)
# ════════════════════════════════════════════════════════════════════════════════
#
# Propósito: Automatizar a execução local do Partner.Modern
# Responsabilidades:
#   1. Validate .env file
#   2. Load environment variables
#   3. Display configuration summary
#   4. Build solution
#   5. Run API server
#
# Uso: .\start_partner_local.ps1
# 
# IMPORTANTE: Se receber erro de execução, execute:
#   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# ════════════════════════════════════════════════════════════════════════════════

param(
    [switch]$SkipBuild = $false,
    [switch]$DryRun = $false
)

# Enable error handling
$ErrorActionPreference = "Stop"

# ════════════════════════════════════════════════════════════════════════════════
# HELPER FUNCTIONS
# ════════════════════════════════════════════════════════════════════════════════

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

# ════════════════════════════════════════════════════════════════════════════════
# STEP 1: Initialize
# ════════════════════════════════════════════════════════════════════════════════

Clear-Host
Write-Host ""
Write-Host "✓ Partner.Modern — Local Bootstrap" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# STEP 2: Check if .env exists
# ════════════════════════════════════════════════════════════════════════════════

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

# ════════════════════════════════════════════════════════════════════════════════
# STEP 3: Load environment variables from .env
# ════════════════════════════════════════════════════════════════════════════════

Write-Header "STEP 2: Loading environment variables"

Write-Info "Parsing .env file..."

$envVars = @{}
$envContent = Get-Content ".env" -ErrorAction SilentlyContinue

foreach ($line in $envContent) {
    $line = $line.Trim()
    
    # Skip comments and empty lines
    if ($line.StartsWith("#") -or $line -eq "") {
        continue
    }
    
    # Split on first '='
    $parts = $line -split '=', 2
    if ($parts.Count -eq 2) {
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        
        # Remove quotes if present
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        
        $envVars[$key] = $value
        [Environment]::SetEnvironmentVariable($key, $value, "Process")
    }
}

Write-Success "Environment variables loaded"
Write-Host "  Found $($envVars.Count) variables" -ForegroundColor Gray

# ════════════════════════════════════════════════════════════════════════════════
# STEP 4: Validate critical configuration
# ════════════════════════════════════════════════════════════════════════════════

Write-Header "STEP 3: Validating configuration"

$validationFailed = $false

# Check ConnectionStrings__PartnerDb
if (-not $envVars.ContainsKey("ConnectionStrings__PartnerDb") -or [string]::IsNullOrWhiteSpace($envVars["ConnectionStrings__PartnerDb"])) {
    Write-Error-Custom "ConnectionStrings__PartnerDb not set in .env"
    $validationFailed = $true
}

# Check Jwt__SecretKey
if (-not $envVars.ContainsKey("Jwt__SecretKey") -or [string]::IsNullOrWhiteSpace($envVars["Jwt__SecretKey"])) {
    Write-Error-Custom "Jwt__SecretKey not set in .env"
    $validationFailed = $true
} elseif ($envVars["Jwt__SecretKey"].StartsWith("CHANGE_ME")) {
    Write-Error-Custom "Jwt__SecretKey contains default placeholder value"
    $validationFailed = $true
} elseif ($envVars["Jwt__SecretKey"].Length -lt 32) {
    Write-Error-Custom "Jwt__SecretKey must be at least 32 characters (current: $($envVars["Jwt__SecretKey"].Length))"
    $validationFailed = $true
}

# Check RabbitMq__Host (can use default)
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

# ════════════════════════════════════════════════════════════════════════════════
# STEP 5: Display configuration summary
# ════════════════════════════════════════════════════════════════════════════════

Write-Header "CONFIGURATION SUMMARY"

Write-Host ""
Write-Host "🗄️  DATABASE" -ForegroundColor Cyan
Write-Host "  SQL Server: $($envVars["ConnectionStrings__PartnerDb"])"

Write-Host ""
Write-Host "🔐 AUTHENTICATION" -ForegroundColor Cyan
Write-Host "  JWT Issuer:       $($envVars["Jwt__Issuer"])"
Write-Host "  JWT Audience:     $($envVars["Jwt__Audience"])"
Write-Host "  JWT Expiration:   $($envVars["Jwt__ExpirationMinutes"] ?? "60") minutes"
Write-Host "  JWT Secret:       [CONFIGURED]" -ForegroundColor Green

Write-Host ""
Write-Host "🐰 RABBITMQ" -ForegroundColor Cyan
Write-Host "  Host:             $($envVars["RabbitMq__Host"])"
Write-Host "  Port:             $($envVars["RabbitMq__Port"] ?? "5672")"
Write-Host "  VirtualHost:      $($envVars["RabbitMq__VirtualHost"] ?? "partner")"
Write-Host "  Username:         $($envVars["RabbitMq__Username"])"
Write-Host "  Import Queue:     $($envVars["RabbitMq__ImportJobsQueue"] ?? "partner.import.jobs")"

Write-Host ""
Write-Host "🌐 VTEX INTEGRATION" -ForegroundColor Cyan
Write-Host "  Enabled:          $($envVars["Vtex__Enabled"] ?? "false")"
if ($envVars["Vtex__Enabled"] -eq "true") {
    Write-Host "  BaseUrl:          $($envVars["Vtex__BaseUrl"])"
    Write-Host "  RetryCount:       $($envVars["Vtex__RetryCount"] ?? "3")"
}

Write-Host ""
Write-Host "🌍 CORS" -ForegroundColor Cyan
Write-Host "  Allowed Origins:  $($envVars["Cors__AllowedOrigins__0"] ?? "http://localhost:4200")"

Write-Host ""
Write-Host "📊 LOGGING" -ForegroundColor Cyan
Write-Host "  Min Level:        $($envVars["Serilog__MinimumLevel__Default"] ?? "Information")"
Write-Host "  Seq Enabled:      $($envVars["Serilog__Seq__Enabled"] ?? "false")"

Write-Host ""
Write-Host "🌐 ENDPOINTS" -ForegroundColor Cyan
Write-Host "  Swagger:          https://localhost:7111/swagger"
Write-Host "  Health:           https://localhost:7111/health"
Write-Host "  Health Ready:     https://localhost:7111/health/ready"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ════════════════════════════════════════════════════════════════════════════════
# STEP 6: Check dotnet is available
# ════════════════════════════════════════════════════════════════════════════════

Write-Header "STEP 4: Checking .NET SDK"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Error-Custom "dotnet CLI not found"
    Write-Host "Please install .NET SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

$dotnetVersion = & dotnet --version
Write-Success "dotnet $dotnetVersion found"

# ════════════════════════════════════════════════════════════════════════════════
# STEP 7: Build solution
# ════════════════════════════════════════════════════════════════════════════════

if ($SkipBuild) {
    Write-Header "SKIPPING BUILD"
    Write-Warning-Custom "Build skipped (--SkipBuild flag)"
} else {
    Write-Header "STEP 5: Building solution"
    
    Write-Info "Building Partner.Modern.sln..."
    Write-Host ""
    
    if ($DryRun) {
        Write-Warning-Custom "DRY RUN: Skipping actual build"
    } else {
        try {
            & dotnet build Partner.Modern.sln --configuration Debug --verbosity minimal
            if ($LASTEXITCODE -ne 0) {
                Write-Host ""
                Write-Error-Custom "BUILD FAILED"
                exit 1
            }
        } catch {
            Write-Error-Custom "Build failed: $_"
            exit 1
        }
    }
    
    Write-Host ""
    Write-Success "Build successful"
}

# ════════════════════════════════════════════════════════════════════════════════
# STEP 8: Run API server
# ════════════════════════════════════════════════════════════════════════════════

Write-Header "STEP 6: Starting Partner.Api server"

Write-Host ""
Write-Host "[READY TO START]" -ForegroundColor Green
Write-Host "  Database:    $($envVars["ConnectionStrings__PartnerDb"])"
Write-Host "  Environment: Development"
Write-Host "  Port:        HTTPS 7111 / HTTP 5205"
Write-Host ""
Write-Host "  Accessing the API at: https://localhost:7111" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Warning-Custom "DRY RUN: Skipping server start"
    exit 0
}

Write-Info "Starting server (press Ctrl+C to stop)..."
Write-Host ""

Push-Location Migracao\Partner.Api

try {
    & dotnet run --configuration Debug --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Error-Custom "Failed to start API server"
        exit 1
    }
} catch {
    Write-Error-Custom "Error starting server: $_"
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
Partner.Modern local bootstrap script for PowerShell.

.SYNOPSIS
Automates the setup and execution of Partner.Modern locally.

.PARAMETER SkipBuild
Skip the build step and go directly to running the server.

.PARAMETER DryRun
Perform validation and display configuration without actually building or running.

.EXAMPLE
.\start_partner_local.ps1

.EXAMPLE
.\start_partner_local.ps1 -SkipBuild

.EXAMPLE
.\start_partner_local.ps1 -DryRun

.NOTES
Requires PowerShell 5.0+ and .NET SDK installed.
If you get an execution policy error, run:
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
#>
