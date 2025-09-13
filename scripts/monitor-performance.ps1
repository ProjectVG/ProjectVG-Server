# ProjectVG API Performance Monitoring Script
# 부하테스트 중 실시간 성능 모니터링 및 로깅

param(
    [string]$ApiUrl = "http://localhost:7804",
    [int]$IntervalSeconds = 5,
    [string]$OutputDir = ".\loadtest-results",
    [switch]$EnableDotnetCounters,
    [switch]$Continuous
)

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

# 출력 디렉토리 생성
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-ColorOutput "Created output directory: $OutputDir" "Green"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $OutputDir "performance-monitor-$timestamp.csv"
$processLogFile = Join-Path $OutputDir "process-monitor-$timestamp.log"

Write-ColorOutput "=== ProjectVG API Performance Monitor ===" "Cyan"
Write-ColorOutput "API URL: $ApiUrl" "White"
Write-ColorOutput "Monitoring Interval: $IntervalSeconds seconds" "White"
Write-ColorOutput "Log File: $logFile" "White"
Write-ColorOutput "Process Log: $processLogFile" "White"

# CSV 헤더 작성
$csvHeader = "Timestamp,WorkingMemoryMB,PrivateMemoryMB,CpuUsage%,ThreadPoolWorkers,ThreadPoolCP,PendingWork,GCGen0,GCGen1,GCGen2,TotalMemoryMB,AvailableThreads,Status"
$csvHeader | Out-File -FilePath $logFile -Encoding UTF8

# API 연결 테스트
try {
    Write-ColorOutput "Testing API connection..." "Yellow"
    $healthCheck = Invoke-RestMethod -Uri "$ApiUrl/health" -TimeoutSec 10
    Write-ColorOutput "API Health Check: OK" "Green"
} catch {
    Write-ColorOutput "API Health Check Failed: $($_.Exception.Message)" "Red"
    Write-ColorOutput "Make sure the API is running in LoadTest environment" "Yellow"
    exit 1
}

# dotnet-counters 프로세스 시작 (선택적)
$dotnetCountersProcess = $null
if ($EnableDotnetCounters) {
    try {
        Write-ColorOutput "Starting dotnet-counters..." "Yellow"
        
        # API 프로세스 ID 찾기
        $apiProcesses = Get-Process -Name "ProjectVG.Api" -ErrorAction SilentlyContinue
        if ($apiProcesses.Count -eq 0) {
            $apiProcesses = Get-Process | Where-Object { $_.ProcessName -like "*ProjectVG*" -or $_.MainWindowTitle -like "*ProjectVG*" }
        }
        
        if ($apiProcesses.Count -gt 0) {
            $processId = $apiProcesses[0].Id
            Write-ColorOutput "Found API Process ID: $processId" "Green"
            
            $countersLogFile = Join-Path $OutputDir "dotnet-counters-$timestamp.txt"
            $dotnetCountersArgs = @(
                "monitor",
                "--process-id", $processId,
                "--refresh-interval", $IntervalSeconds,
                "--format", "table",
                "Microsoft.AspNetCore.Hosting",
                "System.Runtime",
                "Microsoft.AspNetCore.Http.Connections"
            )
            
            $dotnetCountersProcess = Start-Process -FilePath "dotnet-counters" -ArgumentList $dotnetCountersArgs -RedirectStandardOutput $countersLogFile -NoNewWindow -PassThru
            Write-ColorOutput "dotnet-counters started, output: $countersLogFile" "Green"
        } else {
            Write-ColorOutput "Could not find API process for dotnet-counters" "Yellow"
        }
    } catch {
        Write-ColorOutput "Failed to start dotnet-counters: $($_.Exception.Message)" "Red"
    }
}

# 성능 임계값 설정
$thresholds = @{
    MemoryMB = 500
    CpuPercent = 80
    PendingWork = 100
    AvailableThreads = 10
}

Write-ColorOutput "=== Starting Performance Monitoring ===" "Cyan"
Write-ColorOutput "Press Ctrl+C to stop monitoring" "Yellow"

$monitoringStartTime = Get-Date
$alertCount = 0

try {
    do {
        $currentTime = Get-Date
        
        try {
            # API 성능 지표 수집
            $metricsResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/monitoring/metrics" -TimeoutSec 5
            
            # 데이터 추출
            $workingMemory = $metricsResponse.process.workingSetMemoryMB
            $privateMemory = $metricsResponse.process.privateMemoryMB
            $cpuUsage = [math]::Round($metricsResponse.process.cpuUsagePercent, 2)
            $workerThreads = $metricsResponse.threadPool.workerThreads
            $completionPortThreads = $metricsResponse.threadPool.completionPortThreads
            $pendingWork = $metricsResponse.threadPool.pendingWorkItems
            $gcGen0 = $metricsResponse.gc.gen0Collections
            $gcGen1 = $metricsResponse.gc.gen1Collections
            $gcGen2 = $metricsResponse.gc.gen2Collections
            $totalMemory = $metricsResponse.gc.totalMemoryMB
            $availableThreads = $metricsResponse.threadPool.availableThreads
            
            # 상태 결정
            $status = "OK"
            $alerts = @()
            
            if ($workingMemory -gt $thresholds.MemoryMB) {
                $status = "HIGH_MEMORY"
                $alerts += "High Memory Usage: $workingMemory MB"
            }
            
            if ($cpuUsage -gt $thresholds.CpuPercent) {
                $status = "HIGH_CPU" 
                $alerts += "High CPU Usage: $cpuUsage%"
            }
            
            if ($pendingWork -gt $thresholds.PendingWork) {
                $status = "HIGH_QUEUE"
                $alerts += "High Pending Work: $pendingWork"
            }
            
            if ($availableThreads -lt $thresholds.AvailableThreads) {
                $status = "LOW_THREADS"
                $alerts += "Low Available Threads: $availableThreads"
            }
            
            # CSV 로그 기록
            $csvLine = "$($currentTime.ToString('yyyy-MM-dd HH:mm:ss')),$workingMemory,$privateMemory,$cpuUsage,$workerThreads,$completionPortThreads,$pendingWork,$gcGen0,$gcGen1,$gcGen2,$totalMemory,$availableThreads,$status"
            $csvLine | Out-File -FilePath $logFile -Append -Encoding UTF8
            
            # 콘솔 출력
            $elapsed = $currentTime - $monitoringStartTime
            Write-Host "`r[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
            
            $statusColor = switch ($status) {
                "OK" { "Green" }
                default { "Red" }
            }
            
            Write-Host "[$status] " -ForegroundColor $statusColor -NoNewline
            Write-Host "Memory: $workingMemory MB | " -NoNewline
            Write-Host "CPU: $cpuUsage% | " -NoNewline  
            Write-Host "Queue: $pendingWork | " -NoNewline
            Write-Host "Threads: $availableThreads | " -NoNewline
            Write-Host "GC: G0=$gcGen0 G1=$gcGen1 G2=$gcGen2 | " -NoNewline
            Write-Host "Elapsed: $($elapsed.ToString('hh\:mm\:ss'))" -NoNewline
            
            # 알림 처리
            if ($alerts.Count -gt 0) {
                $alertCount++
                Write-Host ""
                foreach ($alert in $alerts) {
                    Write-ColorOutput "  ALERT: $alert" "Red"
                }
                
                # 프로세스 로그에 알림 기록
                $alertLog = "[$($currentTime.ToString('yyyy-MM-dd HH:mm:ss'))] ALERTS: $($alerts -join ', ')"
                $alertLog | Out-File -FilePath $processLogFile -Append -Encoding UTF8
            }
            
        } catch {
            Write-Host "`r[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
            Write-ColorOutput "[ERROR] Failed to get metrics: $($_.Exception.Message)" "Red"
            
            # 에러 로그 기록
            $errorLog = "[$($currentTime.ToString('yyyy-MM-dd HH:mm:ss'))] ERROR: $($_.Exception.Message)"
            $errorLog | Out-File -FilePath $processLogFile -Append -Encoding UTF8
        }
        
        if (!$Continuous) {
            Write-Host ""
        }
        
        Start-Sleep -Seconds $IntervalSeconds
        
    } while ($Continuous)
    
} finally {
    # dotnet-counters 정리
    if ($dotnetCountersProcess -and !$dotnetCountersProcess.HasExited) {
        Write-ColorOutput "`nStopping dotnet-counters..." "Yellow"
        try {
            $dotnetCountersProcess.Kill()
            $dotnetCountersProcess.WaitForExit(5000)
        } catch {
            Write-ColorOutput "Failed to stop dotnet-counters gracefully" "Yellow"
        }
    }
    
    $endTime = Get-Date
    $totalDuration = $endTime - $monitoringStartTime
    
    Write-ColorOutput "`n=== Monitoring Summary ===" "Cyan"
    Write-ColorOutput "Total Duration: $($totalDuration.ToString('hh\:mm\:ss'))" "White"
    Write-ColorOutput "Total Alerts: $alertCount" "White"
    Write-ColorOutput "Log Files:" "White"
    Write-ColorOutput "  Performance: $logFile" "White"
    Write-ColorOutput "  Process: $processLogFile" "White"
    
    if ($EnableDotnetCounters) {
        $countersLogFile = Join-Path $OutputDir "dotnet-counters-$timestamp.txt"
        if (Test-Path $countersLogFile) {
            Write-ColorOutput "  dotnet-counters: $countersLogFile" "White"
        }
    }
}

Write-ColorOutput "Performance monitoring completed." "Green"