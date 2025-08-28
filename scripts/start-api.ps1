# API 빠른 빌드 및 시작 스크립트 (다운타임 최소화)
Write-Host "=== ProjectVG API Fast Build & Start ===" -ForegroundColor Green

# 성공적인 빌드 후에만 기존 컨테이너 중지 (다운타임 최소화)
Write-Host "1. Building new API image (minimizing downtime)..." -ForegroundColor Yellow
docker-compose build --no-cache=false projectvg.api

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# 성공적인 빌드 후에만 기존 컨테이너 중지
Write-Host "2. Stopping existing API container..." -ForegroundColor Yellow
docker-compose stop projectvg.api
docker-compose rm -f projectvg.api

# 새 이미지로 API 시작
Write-Host "3. Starting API container with new image..." -ForegroundColor Yellow
docker-compose up -d projectvg.api

# 상태 확인
Write-Host "4. Checking API status..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

Write-Host "`n=== API Container Status ===" -ForegroundColor Cyan
docker-compose ps

Write-Host "`n=== API Log Commands ===" -ForegroundColor Cyan
Write-Host "API logs: docker logs mainapiserver-projectvg.api-1" -ForegroundColor Gray

Write-Host "`n=== Connection Info ===" -ForegroundColor Cyan
Write-Host "API URL: http://localhost:7910" -ForegroundColor White
Write-Host "Swagger: http://localhost:7910/swagger" -ForegroundColor White

Write-Host "`n=== API Startup Complete! ===" -ForegroundColor Green
