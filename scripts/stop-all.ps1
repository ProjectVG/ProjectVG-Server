# 모든 서비스 중지 스크립트
Write-Host "=== ProjectVG Stop All Services ===" -ForegroundColor Green

# API 서비스 중지
Write-Host "1. Stopping API service..." -ForegroundColor Yellow
docker-compose down --remove-orphans

# DB 및 Redis 서비스 중지
Write-Host "2. Stopping DB and Redis services..." -ForegroundColor Yellow
docker-compose -f docker-compose.db.yml down

# 사용하지 않는 이미지 정리
Write-Host "3. Cleaning up unused images..." -ForegroundColor Yellow
docker image prune -f

# 사용하지 않는 볼륨 정리
Write-Host "4. Cleaning up unused volumes..." -ForegroundColor Yellow
docker volume prune -f

# 사용하지 않는 네트워크 정리
Write-Host "5. Cleaning up unused networks..." -ForegroundColor Yellow
docker network prune -f

Write-Host "`n=== All Services Stopped and Cleaned! ===" -ForegroundColor Green
