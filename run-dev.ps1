# 개발 환경 실행 스크립트
Write-Host "🚀 ProjectVG 개발 환경 시작 중..." -ForegroundColor Green

# 기존 컨테이너 정리
Write-Host "📦 기존 컨테이너 정리 중..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml down

# 개발 환경 빌드 및 실행
Write-Host "🔨 개발 환경 빌드 및 실행 중..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml up --build

Write-Host "✅ 개발 환경이 시작되었습니다!" -ForegroundColor Green
Write-Host "🌐 API: http://localhost:7900" -ForegroundColor Cyan
Write-Host "📚 Swagger: http://localhost:7900/swagger" -ForegroundColor Cyan 