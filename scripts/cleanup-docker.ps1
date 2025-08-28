# Docker 정리 스크립트
Write-Host "=== Docker Cleanup Started ===" -ForegroundColor Green

# 1. 중지된 컨테이너 제거
Write-Host "1. Removing stopped containers..." -ForegroundColor Yellow
$stoppedContainers = docker ps -a -q -f status=exited
if ($stoppedContainers) {
    docker rm $stoppedContainers
    Write-Host "Stopped containers removed successfully" -ForegroundColor Green
} else {
    Write-Host "No stopped containers found" -ForegroundColor Gray
}

# 2. 사용하지 않는 이미지 제거
Write-Host "2. Removing unused images..." -ForegroundColor Yellow
docker image prune -f
Write-Host "Unused images removed successfully" -ForegroundColor Green

# 3. 사용하지 않는 볼륨 제거
Write-Host "3. Removing unused volumes..." -ForegroundColor Yellow
docker volume prune -f
Write-Host "Unused volumes removed successfully" -ForegroundColor Green

# 4. 사용하지 않는 네트워크 제거
Write-Host "4. Removing unused networks..." -ForegroundColor Yellow
docker network prune -f
Write-Host "Unused networks removed successfully" -ForegroundColor Green

# 5. 빌드 캐시 제거
Write-Host "5. Removing build cache..." -ForegroundColor Yellow
docker builder prune -f
Write-Host "Build cache removed successfully" -ForegroundColor Green

# 6. 시스템 전체 정리
Write-Host "6. Performing system-wide cleanup..." -ForegroundColor Yellow
docker system prune -f
Write-Host "System-wide cleanup completed" -ForegroundColor Green

# 7. 현재 상태 표시
Write-Host "`n=== Current Docker Status ===" -ForegroundColor Cyan
Write-Host "Running containers:" -ForegroundColor White
docker ps

Write-Host "`nImages:" -ForegroundColor White
docker images

Write-Host "`nVolumes:" -ForegroundColor White
docker volume ls

Write-Host "`nNetworks:" -ForegroundColor White
docker network ls

Write-Host "`n=== Docker Cleanup Completed! ===" -ForegroundColor Green
