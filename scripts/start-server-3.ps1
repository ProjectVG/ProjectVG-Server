# 서버 3 시작 스크립트 (분산 모드)

Write-Host "🚀 API 서버 3 시작 (분산 모드)" -ForegroundColor Cyan

# 분산 시스템 환경 변수 설정
$env:DISTRIBUTED_MODE = "true"
$env:SERVER_ID = "api-server-003"
$env:REDIS_CONNECTION_STRING = "localhost:6380"

Write-Host "`n🔧 환경 변수:"
Write-Host "   DISTRIBUTED_MODE: $env:DISTRIBUTED_MODE"
Write-Host "   SERVER_ID: $env:SERVER_ID"
Write-Host "   REDIS_CONNECTION_STRING: $env:REDIS_CONNECTION_STRING"
Write-Host "   포트: 7912"

Write-Host "`n📡 서버 3 시작 중..."
dotnet run --project ProjectVG.Api --urls "http://localhost:7912"