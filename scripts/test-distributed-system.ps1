# 분산 시스템 테스트 스크립트

Write-Host "🚀 분산 서버 시스템 테스트 시작" -ForegroundColor Green

# Redis 연결 확인
Write-Host "`n📡 Redis 연결 확인..."
try {
    $redisTest = redis-cli -p 6380 ping
    if ($redisTest -eq "PONG") {
        Write-Host "✅ Redis 연결 성공" -ForegroundColor Green
    } else {
        Write-Host "❌ Redis 연결 실패" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Redis 연결 오류: $_" -ForegroundColor Red
    exit 1
}

# 기존 Redis 데이터 정리
Write-Host "`n🧹 기존 Redis 데이터 정리..."
redis-cli -p 6380 FLUSHALL

# 분산 시스템 환경 변수 설정
$env:DISTRIBUTED_MODE = "true"
$env:REDIS_CONNECTION_STRING = "localhost:6380"

Write-Host "`n🔧 환경 변수 설정:"
Write-Host "   DISTRIBUTED_MODE: $env:DISTRIBUTED_MODE"
Write-Host "   REDIS_CONNECTION_STRING: $env:REDIS_CONNECTION_STRING"

# 테스트 시나리오 안내
Write-Host "`n📋 테스트 시나리오:"
Write-Host "1. 서버 1 시작 (포트 7910)"
Write-Host "2. 서버 2 시작 (포트 7911) - 새 터미널 필요"
Write-Host "3. 서버 3 시작 (포트 7912) - 새 터미널 필요"
Write-Host "4. Redis 상태 모니터링"
Write-Host "5. 클라이언트 테스트"

Write-Host "`n📝 추가 터미널에서 실행할 명령어:"
Write-Host "터미널 2: .\scripts\start-server-2.ps1"
Write-Host "터미널 3: .\scripts\start-server-3.ps1"
Write-Host "터미널 4: .\scripts\monitor-redis.ps1"

Write-Host "`n🚀 서버 1 시작..."
$env:SERVER_ID = "api-server-001"
Write-Host "   SERVER_ID: $env:SERVER_ID"

dotnet run --project ProjectVG.Api --urls "http://localhost:7910"