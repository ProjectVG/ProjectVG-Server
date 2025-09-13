# Quick Performance Monitor for ProjectVG API
# Load test real-time monitoring

param(
    [string]$ApiUrl = "http://localhost:7900",
    [int]$RefreshSeconds = 2
)

function Write-PerformanceBar {
    param([int]$Value, [int]$Max, [string]$Label, [string]$Unit = "")
    
    $percentage = if ($Max -gt 0) { [math]::Min(100, ($Value / $Max) * 100) } else { 0 }
    $barLength = 20
    $filledLength = [math]::Floor(($percentage / 100) * $barLength)
    
    # Use ASCII characters instead of Unicode
    $bar = "#" * $filledLength + "-" * ($barLength - $filledLength)
    
    $color = if ($percentage -gt 80) { "Red" } elseif ($percentage -gt 60) { "Yellow" } else { "Green" }
    
    Write-Host "$Label : " -NoNewline
    Write-Host $bar -ForegroundColor $color -NoNewline
    Write-Host " $Value$Unit/$Max$Unit ($([math]::Round($percentage, 1))%)" -ForegroundColor $color
}

Write-Host "=== ProjectVG API Quick Monitor ===" -ForegroundColor Cyan
Write-Host "API: $ApiUrl" -ForegroundColor White
Write-Host "Press Ctrl+C to exit" -ForegroundColor Yellow
Write-Host ""

$startTime = Get-Date
$previousMetrics = $null

try {
    while ($true) {
        Clear-Host
        $currentTime = Get-Date
        $elapsed = $currentTime - $startTime
        
        Write-Host "=== ProjectVG API Performance Monitor ===" -ForegroundColor Cyan
        Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | Elapsed: $($elapsed.ToString('hh\:mm\:ss'))" -ForegroundColor White
        Write-Host "===============================================================================" -ForegroundColor Gray
        
        try {
            # API metrics collection
            $metrics = Invoke-RestMethod -Uri "$ApiUrl/api/v1/monitoring/metrics" -TimeoutSec 3
            
            # Memory usage
            Write-Host "`nMemory Usage:" -ForegroundColor White
            Write-PerformanceBar $metrics.process.workingSetMemoryMB 1000 "Working Set" "MB"
            Write-PerformanceBar $metrics.gc.totalMemoryMB 500 "GC Memory  " "MB"
            
            # ThreadPool status
            Write-Host "`nThreadPool Status:" -ForegroundColor White
            Write-PerformanceBar $metrics.threadPool.workerThreads 100 "Worker Threads"
            Write-PerformanceBar $metrics.threadPool.pendingWorkItems 50 "Pending Work  "
            
            # GC information
            Write-Host "`nGarbage Collection:" -ForegroundColor White
            
            if ($previousMetrics) {
                $gen0Delta = $metrics.gc.gen0Collections - $previousMetrics.gc.gen0Collections
                $gen1Delta = $metrics.gc.gen1Collections - $previousMetrics.gc.gen1Collections  
                $gen2Delta = $metrics.gc.gen2Collections - $previousMetrics.gc.gen2Collections
                
                Write-Host "Gen 0: $($metrics.gc.gen0Collections) (+$gen0Delta)" -ForegroundColor $(if ($gen0Delta -gt 5) { "Red" } elseif ($gen0Delta -gt 2) { "Yellow" } else { "Green" })
                Write-Host "Gen 1: $($metrics.gc.gen1Collections) (+$gen1Delta)" -ForegroundColor $(if ($gen1Delta -gt 2) { "Red" } elseif ($gen1Delta -gt 0) { "Yellow" } else { "Green" })
                Write-Host "Gen 2: $($metrics.gc.gen2Collections) (+$gen2Delta)" -ForegroundColor $(if ($gen2Delta -gt 0) { "Red" } else { "Green" })
            } else {
                Write-Host "Gen 0: $($metrics.gc.gen0Collections)" -ForegroundColor Green
                Write-Host "Gen 1: $($metrics.gc.gen1Collections)" -ForegroundColor Green
                Write-Host "Gen 2: $($metrics.gc.gen2Collections)" -ForegroundColor Green
            }
            
            # System information
            Write-Host "`nSystem Info:" -ForegroundColor White
            Write-Host "Process ID    : $($metrics.process.id)" -ForegroundColor Gray
            Write-Host "Available Threads: $($metrics.threadPool.availableThreads)" -ForegroundColor $(if ($metrics.threadPool.availableThreads -lt 20) { "Red" } elseif ($metrics.threadPool.availableThreads -lt 50) { "Yellow" } else { "Green" })
            Write-Host "CPU Count     : $($metrics.system.cpuCount)" -ForegroundColor Gray
            
            # Warning indicators
            $warnings = @()
            if ($metrics.process.workingSetMemoryMB -gt 500) { $warnings += "High memory usage" }
            if ($metrics.threadPool.pendingWorkItems -gt 20) { $warnings += "High work queue" }
            if ($metrics.threadPool.availableThreads -lt 20) { $warnings += "Low thread availability" }
            
            if ($warnings.Count -gt 0) {
                Write-Host "`nWarnings:" -ForegroundColor Red
                foreach ($warning in $warnings) {
                    Write-Host "! $warning" -ForegroundColor Red
                }
            } else {
                Write-Host "`nStatus: All systems normal [OK]" -ForegroundColor Green
            }
            
            $previousMetrics = $metrics
            
        } catch {
            Write-Host "`nError: Failed to connect to API" -ForegroundColor Red
            Write-Host "Details: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "`nMake sure:" -ForegroundColor Yellow
            Write-Host "1. API is running on $ApiUrl" -ForegroundColor Yellow
            Write-Host "2. Environment is set to 'LoadTest'" -ForegroundColor Yellow
        }
        
        Write-Host "`n===============================================================================" -ForegroundColor Gray
        Write-Host "Refreshing in $RefreshSeconds seconds... (Ctrl+C to exit)" -ForegroundColor Gray
        
        Start-Sleep -Seconds $RefreshSeconds
    }
} catch [System.Management.Automation.PipelineStoppedException] {
    Write-Host "`nMonitoring stopped by user." -ForegroundColor Yellow
} catch {
    Write-Host "`nMonitoring stopped due to error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Quick monitor session ended." -ForegroundColor Green