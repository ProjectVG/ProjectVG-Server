# 개발 환경 설정 스크립트
Write-Host "=== ProjectVG Development Environment Setup ===" -ForegroundColor Green

# 1. DB 및 Redis 시작
Write-Host "1. Starting DB and Redis..." -ForegroundColor Yellow
& "$PSScriptRoot\start-db.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "DB & Redis startup failed!" -ForegroundColor Red
    exit 1
}

# 2. DB 초기화 대기
Write-Host "2. Waiting for DB initialization (30 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# 3. API 빌드 및 시작
Write-Host "3. Building and starting API..." -ForegroundColor Yellow
& "$PSScriptRoot\start-api.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "API startup failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Development Environment Setup Complete! ===" -ForegroundColor Green
Write-Host "API URL: http://localhost:7910" -ForegroundColor White
Write-Host "Test Client: test-clients/jwt-test-client.html" -ForegroundColor White
