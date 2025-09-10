# ProjectVG Load Test Environment Start Script

Write-Host "Starting ProjectVG load test environment..." -ForegroundColor Green

# Move to project root from current script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

# Check required files exist
if (-not (Test-Path "env.loadtest")) {
    Write-Host "env.loadtest file not found!" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "docker-compose.loadtest.yml")) {
    Write-Host "docker-compose.loadtest.yml file not found!" -ForegroundColor Red
    exit 1
}

# Clean up existing containers
Write-Host "Cleaning up existing load test containers..." -ForegroundColor Yellow
docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml down --remove-orphans 2>$null

# Remove existing images (prevent cache conflicts)
docker rmi projectvg-loadtest-api:latest -f 2>$null
docker rmi projectvg-dummy-llm:latest -f 2>$null  
docker rmi projectvg-dummy-memory:latest -f 2>$null
docker rmi projectvg-dummy-tts:latest -f 2>$null

# Build and start load test environment
Write-Host "Building load test environment..." -ForegroundColor Yellow
docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml build --no-cache 2>$null
docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml up -d

# Wait for services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Check service status
Write-Host "Checking service status..." -ForegroundColor Yellow

$services = @(
    @{Name="LLM Server"; Url="http://localhost:7808/health"},
    @{Name="Memory Server"; Url="http://localhost:7812/health"},
    @{Name="TTS Server"; Url="http://localhost:7816/health"},
    @{Name="Main API"; Url="http://localhost:7804/api/v1/health"}
)

$allHealthy = $true
foreach ($service in $services) {
    try {
        $response = Invoke-RestMethod -Uri $service.Url -TimeoutSec 3
        if ($response.status -eq "ok" -or $response.Status -eq "Healthy") {
            Write-Host "$($service.Name): OK" -ForegroundColor Green
        } else {
            Write-Host "$($service.Name): FAILED" -ForegroundColor Red
            $allHealthy = $false
        }
    } catch {
        Write-Host "$($service.Name): CONNECTION FAILED" -ForegroundColor Red
        $allHealthy = $false
    }
}

if ($allHealthy) {
    Write-Host "`nLoad test environment started successfully!" -ForegroundColor Green
    Write-Host "Service URLs:" -ForegroundColor Cyan
    Write-Host "  - Main API: http://localhost:7804" -ForegroundColor White
    Write-Host "  - LLM Server: http://localhost:7808" -ForegroundColor White  
    Write-Host "  - Memory Server: http://localhost:7812" -ForegroundColor White
    Write-Host "  - TTS Server: http://localhost:7816" -ForegroundColor White
    
    Write-Host "`nYou can now start load testing!" -ForegroundColor Green
    Write-Host "To stop: scripts\stop-loadtest.ps1" -ForegroundColor Yellow
} else {
    Write-Host "`nSome services failed to start" -ForegroundColor Red
    Write-Host "Check logs: docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml logs" -ForegroundColor White
}