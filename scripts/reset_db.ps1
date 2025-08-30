Write-Host "=== ProjectVG DB & Redis Reset ===" -ForegroundColor Green

# 1. 컨테이너 중지 및 볼륨 삭제
Write-Host "1. Stopping and removing containers with volumes..." -ForegroundColor Yellow
docker-compose -f docker-compose.db.yml down -v

# 2. 남아 있는 관련 볼륨 삭제 (안전차원)
Write-Host "2. Removing leftover volumes if any..." -ForegroundColor Yellow
docker volume rm projectvg-db-data 2>$null
docker volume rm projectvg-redis-data 2>$null

# 3. 네트워크 재생성
Write-Host "3. Creating external network..." -ForegroundColor Yellow
docker network create projectvg-external-db 2>$null

# 4. DB 및 Redis 컨테이너 재시작
Write-Host "4. Starting DB and Redis containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.db.yml up -d

# 5. 상태 확인
Write-Host "5. Checking container status..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
docker-compose -f docker-compose.db.yml ps

Write-Host "`n=== Reset Complete! (All DB & Redis data cleared) ===" -ForegroundColor Green
