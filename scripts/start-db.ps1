# DB 및 Redis 시작 스크립트
Write-Host "=== ProjectVG DB & Redis Startup ===" -ForegroundColor Green

# 외부 네트워크 생성 (이미 존재하면 무시)
Write-Host "1. Creating external network..." -ForegroundColor Yellow
docker network create projectvg-external-db 2>$null

# DB 및 Redis 시작
Write-Host "2. Starting DB and Redis containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.db.yml up -d

# 상태 확인
Write-Host "3. Checking container status..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "`n=== Container Status ===" -ForegroundColor Cyan
docker-compose -f docker-compose.db.yml ps

Write-Host "`n=== Log Commands ===" -ForegroundColor Cyan
Write-Host "DB logs: docker logs projectvg-db" -ForegroundColor Gray
Write-Host "Redis logs: docker logs projectvg-redis" -ForegroundColor Gray

Write-Host "`n=== Connection Info ===" -ForegroundColor Cyan
Write-Host "SQL Server: localhost:1433" -ForegroundColor White
Write-Host "Redis: localhost:6380" -ForegroundColor White
Write-Host "Username: sa" -ForegroundColor White
Write-Host "Password: ProjectVG123!" -ForegroundColor White

Write-Host "`n=== DB & Redis Startup Complete! ===" -ForegroundColor Green
