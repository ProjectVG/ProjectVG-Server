# ProjectVG Load Test with Performance Monitoring
# 부하테스트와 성능 모니터링을 동시에 실행

param(
    [string]$LoadTestScript = ".\test-clients\ai-chat-client\script.js",
    [int]$Clients = 10,
    [int]$Duration = 300,  # 5분 기본값
    [string]$ApiUrl = "http://localhost:7900"
)

$ErrorActionPreference = "Stop"

# 색상 출력 함수
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    switch ($Color) {
        "Red" { Write-Host $Message -ForegroundColor Red }
        "Green" { Write-Host $Message -ForegroundColor Green }
        "Yellow" { Write-Host $Message -ForegroundColor Yellow }
        "Cyan" { Write-Host $Message -ForegroundColor Cyan }
        "Magenta" { Write-Host $Message -ForegroundColor Magenta }
        default { Write-Host $Message }
    }
}

# 타임스탬프 생성
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultsDir = ".\loadtest-results\run-$timestamp"

Write-ColorOutput "=== ProjectVG Load Test with Performance Monitoring ===" "Cyan"
Write-ColorOutput "Load Test Script: $LoadTestScript" "White"
Write-ColorOutput "Clients: $Clients" "White"
Write-ColorOutput "Duration: $Duration seconds" "White"
Write-ColorOutput "API URL: $ApiUrl" "White"
Write-ColorOutput "Results Directory: $resultsDir" "White"

# 결과 디렉토리 생성
if (!(Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
    Write-ColorOutput "Created results directory" "Green"
}

# 환경 검증
Write-ColorOutput "`nValidating environment..." "Yellow"

# API 상태 확인
try {
    $healthCheck = Invoke-RestMethod -Uri "$ApiUrl/health" -TimeoutSec 5
    Write-ColorOutput "✓ API is running" "Green"
} catch {
    Write-ColorOutput "✗ API is not accessible: $($_.Exception.Message)" "Red"
    exit 1
}

# LoadTest 환경 확인
try {
    $metricsCheck = Invoke-RestMethod -Uri "$ApiUrl/api/v1/monitoring/metrics" -TimeoutSec 5
    Write-ColorOutput "✓ Performance monitoring available" "Green"
} catch {
    Write-ColorOutput "✗ Performance monitoring not available. Make sure ASPNETCORE_ENVIRONMENT=LoadTest" "Red"
    exit 1
}

# Node.js 및 부하테스트 스크립트 확인
if (!(Test-Path $LoadTestScript)) {
    Write-ColorOutput "✗ Load test script not found: $LoadTestScript" "Red"
    exit 1
}

try {
    node --version | Out-Null
    Write-ColorOutput "✓ Node.js is available" "Green"
} catch {
    Write-ColorOutput "✗ Node.js is not installed or not in PATH" "Red"
    exit 1
}

# 테스트 시작 시간
$testStartTime = Get-Date
Write-ColorOutput "`nStarting load test at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "Cyan"

# 성능 모니터링 시작
Write-ColorOutput "Starting performance monitoring..." "Yellow"
$performanceLogFile = Join-Path $resultsDir "performance-monitor.csv"
$performanceMonitorProcess = Start-Process -FilePath "powershell.exe" `
    -ArgumentList "-File", ".\scripts\monitor-performance.ps1", "-ApiUrl", $ApiUrl, "-IntervalSeconds", "5", "-OutputDir", $resultsDir, "-Continuous" `
    -WindowStyle "Minimized" -PassThru

Write-ColorOutput "Performance monitoring started (PID: $($performanceMonitorProcess.Id))" "Green"

# dotnet-counters 시작 (가능한 경우)
$dotnetCountersProcess = $null
try {
    $apiProcesses = Get-Process -Name "ProjectVG.Api" -ErrorAction SilentlyContinue
    if ($apiProcesses.Count -eq 0) {
        $apiProcesses = Get-Process | Where-Object { $_.ProcessName -like "*ProjectVG*" }
    }
    
    if ($apiProcesses.Count -gt 0) {
        $processId = $apiProcesses[0].Id
        $countersLogFile = Join-Path $resultsDir "dotnet-counters.json"
        
        Write-ColorOutput "Starting dotnet-counters for process $processId..." "Yellow"
        $dotnetCountersProcess = Start-Process -FilePath "dotnet-counters" `
            -ArgumentList "collect", "--process-id", $processId, "--output", $countersLogFile, "--format", "json", "--counters", "Microsoft.AspNetCore.Hosting,System.Runtime" `
            -WindowStyle "Hidden" -PassThru
        
        Write-ColorOutput "dotnet-counters started (PID: $($dotnetCountersProcess.Id))" "Green"
    }
} catch {
    Write-ColorOutput "Could not start dotnet-counters: $($_.Exception.Message)" "Yellow"
}

# 부하테스트 실행
$loadTestLogFile = Join-Path $resultsDir "loadtest-output.log"
Write-ColorOutput "`nStarting load test..." "Yellow"
Write-ColorOutput "Load test output will be saved to: $loadTestLogFile" "White"

try {
    # Node.js 부하테스트 실행
    $loadTestProcess = Start-Process -FilePath "node" `
        -ArgumentList $LoadTestScript, "--clients", $Clients, "--duration", $Duration, "--url", $ApiUrl `
        -RedirectStandardOutput $loadTestLogFile `
        -RedirectStandardError $loadTestLogFile `
        -NoNewWindow -PassThru
    
    Write-ColorOutput "Load test started (PID: $($loadTestProcess.Id))" "Green"
    Write-ColorOutput "Test will run for $Duration seconds..." "White"
    
    # 진행률 표시
    $progressInterval = [math]::Max(1, [math]::Floor($Duration / 20))
    for ($i = 0; $i -lt $Duration; $i += $progressInterval) {
        $remaining = $Duration - $i
        $progress = [math]::Round(($i / $Duration) * 100, 1)
        
        Write-Host "`r[Load Test] Progress: $progress% | Remaining: $remaining seconds | Elapsed: $i seconds" -NoNewline
        
        Start-Sleep -Seconds $progressInterval
        
        # 프로세스가 종료되었는지 확인
        if ($loadTestProcess.HasExited) {
            Write-Host ""
            Write-ColorOutput "Load test process completed early" "Yellow"
            break
        }
    }
    
    Write-Host ""
    
    # 부하테스트 완료 대기
    if (!$loadTestProcess.HasExited) {
        Write-ColorOutput "Waiting for load test to complete..." "Yellow"
        $loadTestProcess.WaitForExit(30000) # 30초 추가 대기
    }
    
    $testEndTime = Get-Date
    $actualDuration = $testEndTime - $testStartTime
    
    Write-ColorOutput "`nLoad test completed in $($actualDuration.ToString('hh\:mm\:ss'))" "Green"
    
} catch {
    Write-ColorOutput "Load test execution failed: $($_.Exception.Message)" "Red"
} finally {
    # 모니터링 프로세스 정리
    Write-ColorOutput "`nStopping monitoring processes..." "Yellow"
    
    if ($performanceMonitorProcess -and !$performanceMonitorProcess.HasExited) {
        try {
            $performanceMonitorProcess.Kill()
            Write-ColorOutput "Performance monitor stopped" "Green"
        } catch {
            Write-ColorOutput "Failed to stop performance monitor" "Yellow"
        }
    }
    
    if ($dotnetCountersProcess -and !$dotnetCountersProcess.HasExited) {
        try {
            $dotnetCountersProcess.Kill()
            Write-ColorOutput "dotnet-counters stopped" "Green"
        } catch {
            Write-ColorOutput "Failed to stop dotnet-counters" "Yellow"
        }
    }
}

# 결과 요약 생성
Write-ColorOutput "`n=== Load Test Results Summary ===" "Cyan"
Write-ColorOutput "Test Duration: $($actualDuration.ToString('hh\:mm\:ss'))" "White"
Write-ColorOutput "Results Directory: $resultsDir" "White"

# 파일 목록
Write-ColorOutput "`nGenerated Files:" "White"
Get-ChildItem -Path $resultsDir | ForEach-Object {
    $sizeKB = [math]::Round($_.Length / 1024, 1)
    Write-ColorOutput "  $($_.Name) ($sizeKB KB)" "Gray"
}

# 간단한 성능 요약 (마지막 성능 지표)
try {
    Write-ColorOutput "`nFinal Performance Metrics:" "White"
    $finalMetrics = Invoke-RestMethod -Uri "$ApiUrl/api/v1/monitoring/metrics" -TimeoutSec 5
    
    Write-ColorOutput "  Memory Usage: $($finalMetrics.process.workingSetMemoryMB) MB" "White"
    Write-ColorOutput "  GC Collections: Gen0=$($finalMetrics.gc.gen0Collections) Gen1=$($finalMetrics.gc.gen1Collections) Gen2=$($finalMetrics.gc.gen2Collections)" "White"
    Write-ColorOutput "  Available Threads: $($finalMetrics.threadPool.availableThreads)" "White"
    Write-ColorOutput "  Pending Work: $($finalMetrics.threadPool.pendingWorkItems)" "White"
    
} catch {
    Write-ColorOutput "Could not retrieve final metrics" "Yellow"
}

Write-ColorOutput "`nLoad test with monitoring completed successfully!" "Green"
Write-ColorOutput "Check the results directory for detailed logs and metrics." "White"