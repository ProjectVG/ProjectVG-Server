# Docker 완전 정리 스크립트 (경고: 모든 미사용 리소스 제거)
Write-Host "=== Docker Complete Cleanup Started ===" -ForegroundColor Red
Write-Host "WARNING: This script will remove ALL unused Docker resources!" -ForegroundColor Yellow
Write-Host "WARNING: All non-running containers, images, volumes, and networks will be deleted!" -ForegroundColor Yellow

# 사용자 확인
$confirmation = Read-Host "`nDo you want to continue? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "Cleanup cancelled." -ForegroundColor Yellow
    exit 0
}

# 1. 모든 컨테이너 중지 및 제거
Write-Host "`n1. Stopping and removing all containers..." -ForegroundColor Yellow
docker stop $(docker ps -aq) 2>$null
docker rm $(docker ps -aq) 2>$null
Write-Host "All containers removed successfully" -ForegroundColor Green

# 2. 모든 이미지 제거
Write-Host "2. Removing all images..." -ForegroundColor Yellow
docker rmi $(docker images -q) -f 2>$null
Write-Host "All images removed successfully" -ForegroundColor Green

# 3. 모든 볼륨 제거
Write-Host "3. Removing all volumes..." -ForegroundColor Yellow
docker volume rm $(docker volume ls -q) 2>$null
Write-Host "All volumes removed successfully" -ForegroundColor Green

# 4. 모든 사용자 정의 네트워크 제거
Write-Host "4. Removing custom networks..." -ForegroundColor Yellow
docker network rm $(docker network ls -q --filter type=custom) 2>$null
Write-Host "Custom networks removed successfully" -ForegroundColor Green

# 5. 모든 빌드 캐시 제거
Write-Host "5. Removing all build cache..." -ForegroundColor Yellow
docker builder prune -a -f
Write-Host "All build cache removed successfully" -ForegroundColor Green

# 6. 시스템 전체 완전 정리
Write-Host "6. Performing complete system cleanup..." -ForegroundColor Yellow
docker system prune -a -f --volumes
Write-Host "Complete system cleanup finished" -ForegroundColor Green

# 7. 최종 상태 표시
Write-Host "`n=== Final Docker Status ===" -ForegroundColor Cyan
Write-Host "Running containers:" -ForegroundColor White
docker ps

Write-Host "`nImages:" -ForegroundColor White
docker images

Write-Host "`nVolumes:" -ForegroundColor White
docker volume ls

Write-Host "`nNetworks:" -ForegroundColor White
docker network ls

# 8. Show disk usage
Write-Host "`n=== Docker Disk Usage ===" -ForegroundColor Cyan
docker system df

Write-Host "`n=== Docker Complete Cleanup Finished! ===" -ForegroundColor Green
Write-Host "You can now start fresh with a clean environment." -ForegroundColor White
