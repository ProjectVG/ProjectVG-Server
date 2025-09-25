# Redis 분산 시스템 모니터링 스크립트

Write-Host "📊 Redis 분산 시스템 모니터링" -ForegroundColor Green

function Show-RedisStatus {
    Write-Host "`n" + "="*50
    Write-Host "📊 Redis 상태 ($(Get-Date -Format 'HH:mm:ss'))" -ForegroundColor Green
    Write-Host "="*50

    # 활성 서버 목록
    Write-Host "`n🖥️ 활성 서버 목록:"
    $activeServers = redis-cli -p 6380 SMEMBERS servers:active
    if ($activeServers) {
        foreach ($server in $activeServers) {
            if ($server) {
                Write-Host "   ✅ $server" -ForegroundColor Green

                # 서버 정보 조회
                $serverInfo = redis-cli -p 6380 GET "servers:active:$server"
                if ($serverInfo) {
                    $serverData = $serverInfo | ConvertFrom-Json -ErrorAction SilentlyContinue
                    if ($serverData) {
                        Write-Host "      시작: $($serverData.StartedAt)"
                        Write-Host "      마지막 헬스체크: $($serverData.LastHeartbeat)"
                        Write-Host "      활성 연결: $($serverData.ActiveConnections)"
                    }
                }
            }
        }
    } else {
        Write-Host "   ❌ 활성 서버 없음" -ForegroundColor Red
    }

    # 사용자 세션 목록
    Write-Host "`n👥 사용자 세션:"
    $userSessions = redis-cli -p 6380 KEYS "user:server:*"
    if ($userSessions) {
        foreach ($session in $userSessions) {
            if ($session) {
                $userId = $session -replace "user:server:", ""
                $serverId = redis-cli -p 6380 GET $session
                Write-Host "   👤 사용자 $userId -> 서버 $serverId" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "   ℹ️ 활성 사용자 세션 없음" -ForegroundColor Gray
    }

    # Redis 메시지 채널
    Write-Host "`n📡 활성 채널:"
    $channels = redis-cli -p 6380 PUBSUB CHANNELS "*"
    if ($channels) {
        foreach ($channel in $channels) {
            if ($channel) {
                $subscribers = redis-cli -p 6380 PUBSUB NUMSUB $channel
                Write-Host "   📻 $channel (구독자: $($subscribers[1]))" -ForegroundColor Cyan
            }
        }
    } else {
        Write-Host "   ℹ️ 활성 채널 없음" -ForegroundColor Gray
    }

    # Redis 메모리 사용량
    Write-Host "`n💾 Redis 메모리:"
    $memoryInfo = redis-cli -p 6380 INFO memory
    $usedMemory = ($memoryInfo | Select-String "used_memory_human:").ToString().Split(":")[1]
    Write-Host "   사용 중: $usedMemory" -ForegroundColor Magenta
}

function Show-LiveMessages {
    Write-Host "`n📡 실시간 메시지 모니터링 시작..." -ForegroundColor Yellow
    Write-Host "   Ctrl+C로 중지"
    redis-cli -p 6380 MONITOR
}

Write-Host "🎛️ Redis 모니터링 도구"
Write-Host "1. 상태 모니터링 (5초마다 갱신)"
Write-Host "2. 실시간 메시지 모니터링"
Write-Host "3. 한 번만 상태 확인"

$choice = Read-Host "`n선택하세요 (1-3)"

switch ($choice) {
    "1" {
        Write-Host "`n🔄 상태 모니터링 시작 (Ctrl+C로 중지)..."
        while ($true) {
            Clear-Host
            Show-RedisStatus
            Start-Sleep -Seconds 5
        }
    }
    "2" {
        Show-LiveMessages
    }
    "3" {
        Show-RedisStatus
        Write-Host "`n✅ 상태 확인 완료"
    }
    default {
        Show-RedisStatus
    }
}