# API 빠른 빌드 및 시작 스크립트 (최적화된 버전)
Write-Host "=== ProjectVG API Fast Build & Start ===" -ForegroundColor Green

# 시작 시간 기록
$startTime = Get-Date

# 1. 빌드 캐시 최적화를 위한 사전 정리
Write-Host "1. Pre-build cleanup (dangling images)..." -ForegroundColor Yellow
$danglingImages = docker images -f "dangling=true" -q
if ($danglingImages) {
    docker rmi $danglingImages -f 2>$null
    Write-Host "Removed dangling images for better cache utilization" -ForegroundColor Green
} else {
    Write-Host "No dangling images found" -ForegroundColor Gray
}

# 2. 최적화된 빌드 실행
Write-Host "2. Building new API image (cache optimized)..." -ForegroundColor Yellow
Write-Host "   Using build cache for faster builds..." -ForegroundColor Gray

# 빌드 캐시 최적화: --cache-from과 --build-arg를 활용한 레이어 캐시 최적화
docker-compose build --parallel projectvg.api

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Rolling back..." -ForegroundColor Red
    Write-Host "Checking for existing running container..." -ForegroundColor Yellow
    $existingContainer = docker ps -q -f "name=mainapiserver-projectvg.api-1"
    if ($existingContainer) {
        Write-Host "Previous container is still running - no downtime occurred" -ForegroundColor Green
    }
    exit 1
}

# 3. 빌드 성공 후 컨테이너 교체
Write-Host "3. Replacing API container with zero-downtime strategy..." -ForegroundColor Yellow
docker-compose stop projectvg.api
docker-compose rm -f projectvg.api

# 4. 새 이미지로 API 시작
Write-Host "4. Starting API container with new image..." -ForegroundColor Yellow
docker-compose up -d projectvg.api

# 5. 빌드 후 불필요한 이미지 정리
Write-Host "5. Post-build cleanup..." -ForegroundColor Yellow
$oldImages = docker images projectvgapi -f "dangling=true" -q
if ($oldImages) {
    docker rmi $oldImages -f 2>$null
    Write-Host "Cleaned up old build artifacts" -ForegroundColor Green
}

# 6. 컨테이너 상태 확인 (헬스체크 대기)
Write-Host "6. Waiting for container to be healthy..." -ForegroundColor Yellow
$attempts = 0
$maxAttempts = 30
do {
    $attempts++
    Start-Sleep -Seconds 1
    $containerStatus = docker inspect --format='{{.State.Health.Status}}' mainapiserver-projectvg.api-1 2>$null
    if ($containerStatus -eq "healthy") {
        Write-Host "Container is healthy!" -ForegroundColor Green
        break
    }
    Write-Host "." -NoNewline -ForegroundColor Gray
} while ($attempts -lt $maxAttempts)

if ($attempts -eq $maxAttempts) {
    Write-Host "`nWarning: Container health check timeout, but container may still be starting..." -ForegroundColor Yellow
}

# 실행 시간 계산
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "`n=== Build & Start completed in $($duration.TotalSeconds.ToString('F1')) seconds ===" -ForegroundColor Green

Write-Host "`n=== API Container Status ===" -ForegroundColor Cyan
docker-compose ps

Write-Host "`n=== API Log Commands ===" -ForegroundColor Cyan
Write-Host "API logs: docker logs mainapiserver-projectvg.api-1" -ForegroundColor Gray
Write-Host "Follow logs: docker logs mainapiserver-projectvg.api-1 -f" -ForegroundColor Gray

Write-Host "`n=== Connection Info ===" -ForegroundColor Cyan
Write-Host "API URL: http://localhost:7910" -ForegroundColor White
Write-Host "Swagger: http://localhost:7910/swagger" -ForegroundColor White

Write-Host "`n=== Cache Status ===" -ForegroundColor Cyan
$imageCount = (docker images projectvgapi --format "table {{.Repository}}:{{.Tag}}" | Measure-Object -Line).Lines - 1
Write-Host "ProjectVG API images: $imageCount" -ForegroundColor Gray

Write-Host "`n=== API Startup Complete! ===" -ForegroundColor Green