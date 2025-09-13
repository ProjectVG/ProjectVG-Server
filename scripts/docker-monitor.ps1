# Docker Container Performance Monitoring Script
# 컨테이너에서 실행 중인 API의 성능 모니터링

param(
    [string]$ContainerName = "projectvg-loadtest-projectvg-loadtest-api-1",
    [string]$MonitoringType = "counters",  # counters, trace, dump, gcdump
    [int]$Duration = 60,  # 모니터링 지속 시간 (초)
    [string]$OutputDir = ".\loadtest-results"
)

# 색상 출력 함수
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    
    switch ($Color) {
        "Red" { Write-Host $Message -ForegroundColor Red }
        "Green" { Write-Host $Message -ForegroundColor Green }
        "Yellow" { Write-Host $Message -ForegroundColor Yellow }
        "Cyan" { Write-Host $Message -ForegroundColor Cyan }
        default { Write-Host $Message }
    }
}

Write-ColorOutput "=== Docker Container Performance Monitor ===" "Cyan"
Write-ColorOutput "Container: $ContainerName" "White"
Write-ColorOutput "Monitoring Type: $MonitoringType" "White"
Write-ColorOutput "Duration: $Duration seconds" "White"
Write-ColorOutput "Output Directory: $OutputDir" "White"

# 출력 디렉토리 생성
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-ColorOutput "Created output directory: $OutputDir" "Green"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

# 컨테이너 상태 확인
Write-ColorOutput "`nChecking container status..." "Yellow"
try {
    $containerStatus = docker ps -f name=$ContainerName --format "table {{.ID}}\t{{.Names}}\t{{.Status}}"
    if ($containerStatus -like "*$ContainerName*") {
        Write-ColorOutput "Container is running" "Green"
        Write-Host $containerStatus
    } else {
        Write-ColorOutput "Container not found or not running" "Red"
        Write-ColorOutput "Available containers:" "Yellow"
        docker ps --format "table {{.Names}}\t{{.Status}}"
        exit 1
    }
} catch {
    Write-ColorOutput "Failed to check container status: $($_.Exception.Message)" "Red"
    exit 1
}

# API 프로세스 ID 가져오기
Write-ColorOutput "`nGetting API process information..." "Yellow"
try {
    $processInfo = docker exec $ContainerName ps aux | Select-String "dotnet.*ProjectVG.Api.dll"
    if ($processInfo) {
        $processParts = $processInfo -split '\s+' 
        $processId = $processParts[1]
        Write-ColorOutput "Found API process ID: $processId" "Green"
    } else {
        Write-ColorOutput "Could not find API process" "Red"
        Write-ColorOutput "Available processes in container:" "Yellow"
        docker exec $ContainerName ps aux
        exit 1
    }
} catch {
    Write-ColorOutput "Failed to get process information: $($_.Exception.Message)" "Red"
    exit 1
}

# 모니터링 실행
Write-ColorOutput "`nStarting $MonitoringType monitoring..." "Yellow"

switch ($MonitoringType.ToLower()) {
    "counters" {
        $outputFile = Join-Path $OutputDir "docker-counters-$timestamp.txt"
        Write-ColorOutput "Output file: $outputFile" "White"
        
        # dotnet-counters를 컨테이너에서 실행하고 출력을 로컬에 저장
        $countersCommand = "/root/.dotnet/tools/dotnet-counters monitor $processId --refresh-interval 1 --format table --counters Microsoft.AspNetCore.Hosting,System.Runtime,Microsoft.AspNetCore.Http.Connections"
        
        Write-ColorOutput "Running: $countersCommand" "Gray"
        Write-ColorOutput "Press Ctrl+C to stop monitoring" "Yellow"
        
        try {
            # PowerShell에서 직접 docker exec 실행
            Write-ColorOutput "Starting monitoring for $Duration seconds..." "Yellow"
            
            $job = Start-Job -ScriptBlock {
                param($containerName, $command)
                docker exec $containerName bash -c $command
            } -ArgumentList $ContainerName, $countersCommand
            
            # 지정된 시간 후 작업 종료
            Start-Sleep -Seconds $Duration
            Stop-Job $job
            $result = Receive-Job $job
            Remove-Job $job
            
            # 결과를 파일에 저장
            $result | Out-File -FilePath $outputFile -Encoding UTF8
            Write-ColorOutput "Monitoring completed. Output saved to: $outputFile" "Green"
            
        } catch {
            Write-ColorOutput "Monitoring failed: $($_.Exception.Message)" "Red"
        }
    }
    
    "trace" {
        $outputFile = Join-Path $OutputDir "docker-trace-$timestamp.nettrace"
        Write-ColorOutput "Output file: $outputFile" "White"
        
        # dotnet-trace로 트레이스 수집
        $traceCommand = "dotnet-trace collect --process-id $processId --duration 00:00:$($Duration.ToString('D2')) --format NetTrace --output /app/logs/trace-$timestamp.nettrace"
        
        Write-ColorOutput "Running: $traceCommand" "Gray"
        
        try {
            docker exec $ContainerName bash -c $traceCommand
            # 컨테이너에서 로컬로 파일 복사
            docker cp "${ContainerName}:/app/logs/trace-$timestamp.nettrace" $outputFile
            Write-ColorOutput "Trace collection completed: $outputFile" "Green"
        } catch {
            Write-ColorOutput "Trace collection failed: $($_.Exception.Message)" "Red"
        }
    }
    
    "dump" {
        $outputFile = Join-Path $OutputDir "docker-dump-$timestamp.dmp"
        Write-ColorOutput "Output file: $outputFile" "White"
        
        # dotnet-dump로 메모리 덤프 생성
        $dumpCommand = "dotnet-dump collect --process-id $processId --output /app/logs/dump-$timestamp.dmp"
        
        Write-ColorOutput "Running: $dumpCommand" "Gray"
        
        try {
            docker exec $ContainerName bash -c $dumpCommand
            # 컨테이너에서 로컬로 파일 복사
            docker cp "${ContainerName}:/app/logs/dump-$timestamp.dmp" $outputFile
            Write-ColorOutput "Memory dump completed: $outputFile" "Green"
        } catch {
            Write-ColorOutput "Memory dump failed: $($_.Exception.Message)" "Red"
        }
    }
    
    "gcdump" {
        $outputFile = Join-Path $OutputDir "docker-gcdump-$timestamp.gcdump"
        Write-ColorOutput "Output file: $outputFile" "White"
        
        # dotnet-gcdump로 GC 덤프 생성
        $gcdumpCommand = "dotnet-gcdump collect --process-id $processId --output /app/logs/gcdump-$timestamp.gcdump"
        
        Write-ColorOutput "Running: $gcdumpCommand" "Gray"
        
        try {
            docker exec $ContainerName bash -c $gcdumpCommand
            # 컨테이너에서 로컬로 파일 복사
            docker cp "${ContainerName}:/app/logs/gcdump-$timestamp.gcdump" $outputFile
            Write-ColorOutput "GC dump completed: $outputFile" "Green"
        } catch {
            Write-ColorOutput "GC dump failed: $($_.Exception.Message)" "Red"
        }
    }
    
    default {
        Write-ColorOutput "Unknown monitoring type: $MonitoringType" "Red"
        Write-ColorOutput "Available types: counters, trace, dump, gcdump" "Yellow"
        exit 1
    }
}

# 컨테이너 리소스 사용량 표시
Write-ColorOutput "`nContainer resource usage:" "White"
try {
    $containerStats = docker stats $ContainerName --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}\t{{.NetIO}}\t{{.BlockIO}}"
    Write-Host $containerStats
} catch {
    Write-ColorOutput "Could not get container stats" "Yellow"
}

# API 성능 지표도 수집 (가능한 경우)
Write-ColorOutput "`nTrying to get API performance metrics..." "White"
try {
    $apiMetrics = Invoke-RestMethod -Uri "http://localhost:7804/api/v1/monitoring/metrics" -TimeoutSec 5
    
    Write-ColorOutput "API Performance Summary:" "Green"
    Write-ColorOutput "  Memory Usage: $($apiMetrics.process.workingSetMemoryMB) MB" "White"
    Write-ColorOutput "  Available Threads: $($apiMetrics.threadPool.availableThreads)" "White"
    Write-ColorOutput "  Pending Work: $($apiMetrics.threadPool.pendingWorkItems)" "White"
    Write-ColorOutput "  GC Collections: Gen0=$($apiMetrics.gc.gen0Collections) Gen1=$($apiMetrics.gc.gen1Collections) Gen2=$($apiMetrics.gc.gen2Collections)" "White"
} catch {
    Write-ColorOutput "Could not retrieve API metrics (API might not be in LoadTest mode)" "Yellow"
}

Write-ColorOutput "`nDocker performance monitoring completed!" "Green"

# 사용법 출력
Write-ColorOutput "`nUsage Examples:" "Cyan"
Write-ColorOutput "  Monitor counters: .\scripts\docker-monitor.ps1 -MonitoringType counters -Duration 60" "Gray"
Write-ColorOutput "  Collect trace: .\scripts\docker-monitor.ps1 -MonitoringType trace -Duration 30" "Gray"
Write-ColorOutput "  Memory dump: .\scripts\docker-monitor.ps1 -MonitoringType dump" "Gray"
Write-ColorOutput "  GC dump: .\scripts\docker-monitor.ps1 -MonitoringType gcdump" "Gray"