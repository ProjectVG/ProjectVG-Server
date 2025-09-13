# 개발 환경 설정 스크립트
Write-Host "=== ProjectVG Development Environment Setup ===" -ForegroundColor Green

# 1. DB 및 Redis 시작
Write-Host "1. Starting DB and Redis..." -ForegroundColor Yellow
& "$PSScriptRoot\start-db.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "DB & Redis startup failed!" -ForegroundColor Red
    exit 1
}

# 2. DB 초기화 대기
Write-Host "2. Waiting for DB and Redis to be ready..." -ForegroundColor Yellow

function Wait-ForUrl {
    param([string]$url, [int]$timeoutSec=120)
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopwatch.Elapsed.TotalSeconds -lt $timeoutSec) {
        try {
            $resp = Invoke-WebRequest -Uri $url -TimeoutSec 3 -UseBasicParsing
            if ($resp.StatusCode -eq 200) { return $true }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    return $false
}

# Redis 연결 확인 (API를 통해)
Write-Host "Checking Redis connectivity..." -ForegroundColor Cyan
$redisReady = $false
$maxWait = 60
$elapsed = 0
while (-not $redisReady -and $elapsed -lt $maxWait) {
    try {
        # Redis가 준비되었는지 간접적으로 확인 (포트 확인)
        $redisConnection = Test-NetConnection -ComputerName localhost -Port 6380 -WarningAction SilentlyContinue
        if ($redisConnection.TcpTestSucceeded) {
            Write-Host "Redis is ready!" -ForegroundColor Green
            $redisReady = $true
        } else {
            Start-Sleep -Seconds 2
            $elapsed += 2
        }
    } catch {
        Start-Sleep -Seconds 2
        $elapsed += 2
    }
}

if (-not $redisReady) {
    Write-Host "Warning: Redis readiness check failed, continuing anyway..." -ForegroundColor Yellow
}

# 3. API 빌드 및 시작
Write-Host "3. Building and starting API..." -ForegroundColor Yellow
& "$PSScriptRoot\start-api.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "API startup failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Development Environment Setup Complete! ===" -ForegroundColor Green
Write-Host "API URL: http://localhost:7910" -ForegroundColor White
Write-Host "Test Client: test-clients/jwt-test-client.html" -ForegroundColor White
