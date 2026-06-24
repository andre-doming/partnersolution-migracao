@echo off
REM ════════════════════════════════════════════════════════════════════════════════
REM PARTNER.IMPORT.WORKER — LOCAL BOOTSTRAP SCRIPT (Windows CMD)
REM ════════════════════════════════════════════════════════════════════════════════
REM
REM Propósito: Automatizar a execução local do Partner.Import.Worker
REM Responsabilidades:
REM   1. Validate .env file
REM   2. Load environment variables
REM   3. Display configuration summary
REM   4. Build worker
REM   5. Run worker
REM
REM Uso: start_partner_worker.cmd
REM ════════════════════════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

cls
color 0A

echo.
echo ════════════════════════════════════════════════════════════════════════════════
echo  ✓ Partner.Import.Worker — Local Bootstrap
echo ════════════════════════════════════════════════════════════════════════════════
echo.

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 1: Check if .env exists
REM ════════════════════════════════════════════════════════════════════════════════

if not exist ".env" (
    echo [⚠️  WARNING] .env file not found!
    echo.
    echo Creating .env from .env.example...
    if exist ".env.example" (
        copy ".env.example" ".env" >nul
        echo [✓] .env created successfully!
        echo.
        echo ⚠️  IMPORTANT: Edit .env with your environment variables before running
        echo.
        pause
    ) else (
        echo [❌ ERROR] .env.example not found!
        exit /b 1
    )
)

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 2: Load environment variables from .env
REM ════════════════════════════════════════════════════════════════════════════════

echo [*] Loading environment variables from .env...

for /f "tokens=1,* delims==" %%A in ('findstr /v "^#" .env ^| findstr /v "^$"') do (
    set "%%A=%%B"
)

echo [✓] Environment variables loaded

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 3: Validate critical configuration
REM ════════════════════════════════════════════════════════════════════════════════

echo.
echo [*] Validating critical configuration...

set VALIDATION_FAILED=false

if "!ConnectionStrings__PartnerDb!"=="" (
    echo [❌ ERROR] ConnectionStrings__PartnerDb not set in .env
    set VALIDATION_FAILED=true
)

if "!RabbitMq__Host!"=="" (
    echo [⚠️  WARNING] RabbitMq__Host not set, using default: localhost
    set "RabbitMq__Host=localhost"
)

if !VALIDATION_FAILED!==true (
    echo.
    echo [❌ VALIDATION FAILED]
    echo Please configure all required variables in .env file
    echo.
    exit /b 1
)

echo [✓] Configuration validation passed

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 4: Display configuration summary
REM ════════════════════════════════════════════════════════════════════════════════

echo.
echo ════════════════════════════════════════════════════════════════════════════════
echo  CONFIGURATION SUMMARY
echo ════════════════════════════════════════════════════════════════════════════════

echo.
echo 🗄️  DATABASE
echo   SQL Server: !ConnectionStrings__PartnerDb!

echo.
echo 🐰 RABBITMQ
echo   Host:             !RabbitMq__Host!
echo   Port:             !RabbitMq__Port!
echo   VirtualHost:      !RabbitMq__VirtualHost!
echo   Username:         !RabbitMq__Username!
echo   Import Queue:     !RabbitMq__ImportJobsQueue!

echo.
echo 📊 LOGGING
echo   Min Level:        !Serilog__MinimumLevel__Default!
echo   Seq Enabled:      !Serilog__Seq__Enabled!

echo.
echo ════════════════════════════════════════════════════════════════════════════════

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 5: Check dotnet is available
REM ════════════════════════════════════════════════════════════════════════════════

echo.
echo [*] Checking dotnet CLI...

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [❌ ERROR] dotnet CLI not found
    echo Please install .NET SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [✓] dotnet %DOTNET_VERSION% found

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 6: Build worker
REM ════════════════════════════════════════════════════════════════════════════════

echo.
echo ════════════════════════════════════════════════════════════════════════════════
echo  [*] Building Partner.Import.Worker...
echo ════════════════════════════════════════════════════════════════════════════════

dotnet build Migracao\Partner.Import.Worker\Partner.Import.Worker.csproj --configuration Debug --verbosity minimal
if %errorlevel% neq 0 (
    echo.
    echo [❌ BUILD FAILED]
    exit /b 1
)

echo.
echo [✓] Build successful

REM ════════════════════════════════════════════════════════════════════════════════
REM STEP 7: Run worker
REM ════════════════════════════════════════════════════════════════════════════════

echo.
echo ════════════════════════════════════════════════════════════════════════════════
echo  [*] Starting Partner.Import.Worker...
echo ════════════════════════════════════════════════════════════════════════════════
echo.

echo [READY TO START]
echo   Database:    !ConnectionStrings__PartnerDb!
echo   Environment: Development
echo.

cd Migracao\Partner.Import.Worker
dotnet run --configuration Debug --no-build

if %errorlevel% neq 0 (
    echo.
    echo [❌ ERROR] Failed to start Partner.Import.Worker
    exit /b 1
)

exit /b 0