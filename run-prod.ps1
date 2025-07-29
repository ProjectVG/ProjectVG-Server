# 프로덕션 환경 실행 스크립트
Write-Host "🚀 ProjectVG 프로덕션 환경 시작 중..." -ForegroundColor Green

# 1. 새 이미지 빌드 (기존 컨테이너는 그대로 유지)
Write-Host "🔨 새 이미지 빌드 중..." -ForegroundColor Yellow
docker-compose build --no-cache

# 2. 빌드 성공 확인
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 이미지 빌드 실패!" -ForegroundColor Red
    exit 1
}

# 3. 기존 컨테이너 중지 및 제거
Write-Host "📦 기존 컨테이너 교체 중..." -ForegroundColor Yellow
docker-compose down

# 4. 새 컨테이너 시작
Write-Host "🚀 새 컨테이너 시작 중..." -ForegroundColor Yellow
docker-compose up -d

# 5. 시작 확인
Write-Host "⏳ 컨테이너 시작 대기 중..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 6. 상태 확인
$containers = docker-compose ps -q
if ($containers) {
    Write-Host "✅ 프로덕션 환경이 성공적으로 시작되었습니다!" -ForegroundColor Green
    Write-Host "🌐 API: http://localhost:7900" -ForegroundColor Cyan
    Write-Host "📚 Swagger: http://localhost:7900/swagger" -ForegroundColor Cyan
    Write-Host "📊 컨테이너 상태:" -ForegroundColor Cyan
    docker-compose ps
} else {
    Write-Host "❌ 컨테이너 시작 실패!" -ForegroundColor Red
    exit 1
} 