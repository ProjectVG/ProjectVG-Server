# ProjectVG Load Test Environment Shutdown Script

Write-Host "Stopping ProjectVG Load Test Environment..." -ForegroundColor Yellow

# Navigate to project root from current script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "Project Root: $projectRoot" -ForegroundColor Yellow

# Stop load test containers
Write-Host "Stopping load test containers..." -ForegroundColor Yellow
docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml down --remove-orphans

# Confirm image cleanup
$cleanupImages = Read-Host "Do you want to delete load test images as well? (y/N)"
if ($cleanupImages -eq "y" -or $cleanupImages -eq "Y") {
    Write-Host "Removing load test images..." -ForegroundColor Yellow
    
    # Remove load test related images
    $imagesToRemove = @(
        "projectvg-loadtest-api:latest",
        "projectvg-dummy-llm:latest",
        "projectvg-dummy-memory:latest", 
        "projectvg-dummy-tts:latest"
    )
    
    foreach ($image in $imagesToRemove) {
        try {
            docker rmi $image -f
            Write-Host "✅ $image image removed successfully" -ForegroundColor Green
        } catch {
            Write-Host "⚠️ $image image removal failed (not found or in use)" -ForegroundColor Yellow
        }
    }
    
    # Clean up unused images
    Write-Host "Cleaning up unused Docker images..." -ForegroundColor Yellow
    docker image prune -f
}

# Clean up network
Write-Host "Cleaning up load test network..." -ForegroundColor Yellow
try {
    docker network rm projectvg-loadtest-network
    Write-Host "✅ Load test network removed successfully" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Load test network removal failed (not found or in use)" -ForegroundColor Yellow
}

Write-Host "`n✅ Load test environment stopped successfully!" -ForegroundColor Green
Write-Host "To restart, run scripts\start-loadtest.ps1" -ForegroundColor Cyan