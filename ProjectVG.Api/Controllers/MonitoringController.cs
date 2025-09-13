using Microsoft.AspNetCore.Mvc;
using ProjectVG.Api.Services;
using System.Diagnostics;
using System.Runtime;

namespace ProjectVG.Api.Controllers;

[ApiController]
[Route("api/v1/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly PerformanceCounterService? _performanceService;
    private readonly ILogger<MonitoringController> _logger;
    private readonly IWebHostEnvironment _environment;

    public MonitoringController(
        ILogger<MonitoringController> logger, 
        IWebHostEnvironment environment,
        PerformanceCounterService? performanceService = null)
    {
        _logger = logger;
        _environment = environment;
        _performanceService = performanceService;
    }

    /// <summary>
    /// 실시간 성능 지표 조회 (LoadTest 환경 전용)
    /// </summary>
    [HttpGet("metrics")]
    public ActionResult<object> GetMetrics()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "Performance monitoring is only available in LoadTest environment" });
        }

        if (_performanceService == null)
        {
            return ServiceUnavailable(new { error = "PerformanceCounterService not available" });
        }

        try
        {
            var metrics = _performanceService.GetCurrentMetrics();
            return Ok(new
            {
                timestamp = metrics.Timestamp,
                environment = _environment.EnvironmentName,
                process = new
                {
                    id = metrics.ProcessId,
                    workingSetMemoryMB = metrics.WorkingSetMemoryMB,
                    privateMemoryMB = metrics.PrivateMemoryMB,
                    virtualMemoryMB = metrics.VirtualMemoryMB,
                    cpuUsagePercent = metrics.CpuUsagePercent
                },
                threadPool = new
                {
                    workerThreads = metrics.ThreadPoolWorkerThreads,
                    completionPortThreads = metrics.ThreadPoolCompletionPortThreads,
                    pendingWorkItems = metrics.ThreadPoolPendingWorkItems,
                    availableThreads = metrics.AvailableThreads
                },
                gc = new
                {
                    gen0Collections = metrics.GCGen0Collections,
                    gen1Collections = metrics.GCGen1Collections,
                    gen2Collections = metrics.GCGen2Collections,
                    totalAllocatedBytesMB = metrics.TotalAllocatedBytes / 1024 / 1024,
                    totalMemoryMB = metrics.TotalMemoryMB
                },
                system = new
                {
                    cpuCount = metrics.SystemCpuCount,
                    machineName = metrics.MachineName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// 빠른 성능 지표 조회 (캐시된 데이터)
    /// </summary>
    [HttpGet("metrics/quick")]
    public ActionResult<object> GetQuickMetrics()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "Performance monitoring is only available in LoadTest environment" });
        }

        if (_performanceService == null)
        {
            return ServiceUnavailable(new { error = "PerformanceCounterService not available" });
        }

        try
        {
            var quickMetrics = _performanceService.GetQuickMetrics();
            return Ok(quickMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quick metrics");
            return StatusCode(500, new { error = "Failed to retrieve quick metrics" });
        }
    }

    /// <summary>
    /// 상세 헬스체크 (성능 지표 포함)
    /// </summary>
    [HttpGet("health-detailed")]
    public ActionResult<object> GetDetailedHealth()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "Detailed health monitoring is only available in LoadTest environment" });
        }

        var currentProcess = Process.GetCurrentProcess();
        
        try
        {
            // GC 상태 확인
            var gcPressure = GC.GetTotalMemory(false) > 100 * 1024 * 1024; // 100MB 이상
            
            // ThreadPool 상태 확인
            ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
            var threadPoolPressure = workerThreads < 10 || completionPortThreads < 10;
            
            // 메모리 상태 확인
            var memoryPressure = currentProcess.WorkingSet64 > 500 * 1024 * 1024; // 500MB 이상
            
            var status = "healthy";
            var warnings = new List<string>();
            
            if (gcPressure) 
            {
                status = "degraded";
                warnings.Add("High GC memory pressure detected");
            }
            
            if (threadPoolPressure) 
            {
                status = "degraded";
                warnings.Add("Low ThreadPool thread availability");
            }
            
            if (memoryPressure) 
            {
                status = "degraded";
                warnings.Add("High memory usage detected");
            }

            return Ok(new
            {
                status,
                timestamp = DateTime.UtcNow,
                environment = _environment.EnvironmentName,
                uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                warnings = warnings,
                details = new
                {
                    processId = currentProcess.Id,
                    workingSetMB = currentProcess.WorkingSet64 / 1024 / 1024,
                    gcTotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                    availableWorkerThreads = workerThreads,
                    availableCompletionPortThreads = completionPortThreads,
                    threadPoolPendingWork = ThreadPool.PendingWorkItemCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get detailed health status");
            return StatusCode(500, new 
            { 
                status = "unhealthy", 
                error = "Health check failed", 
                details = ex.Message 
            });
        }
        finally
        {
            currentProcess?.Dispose();
        }
    }

    /// <summary>
    /// ASP.NET Core 카운터 정보 조회
    /// </summary>
    [HttpGet("counters")]
    public ActionResult<object> GetCounters()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "Counter monitoring is only available in LoadTest environment" });
        }

        if (_performanceService == null)
        {
            return ServiceUnavailable(new { error = "PerformanceCounterService not available" });
        }

        try
        {
            var dotnetCountersCommand = _performanceService.GetDotNetCountersCommand();
            
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                dotnetCountersCommand,
                availableCounters = new[]
                {
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Http.Connections", 
                    "System.Runtime",
                    "Microsoft.AspNetCore.Server.Kestrel"
                },
                usage = new
                {
                    command = "dotnet tool install --global dotnet-counters",
                    monitor = dotnetCountersCommand,
                    export = $"dotnet-counters collect --process-id {Process.GetCurrentProcess().Id} --output loadtest-metrics.json --format json"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get counters info");
            return StatusCode(500, new { error = "Failed to get counters information" });
        }
    }

    /// <summary>
    /// GC 및 메모리 상세 정보
    /// </summary>
    [HttpGet("gc")]
    public ActionResult<object> GetGCInfo()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "GC monitoring is only available in LoadTest environment" });
        }

        try
        {
            // GC 정보 수집 전 강제 GC 실행 (선택적)
            var forceGC = Request.Query.ContainsKey("force");
            if (forceGC)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var gcInfo = new
            {
                timestamp = DateTime.UtcNow,
                forced = forceGC,
                memory = new
                {
                    totalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                    totalAllocatedBytesMB = GC.GetTotalAllocatedBytes(false) / 1024 / 1024,
                    maxGeneration = GC.MaxGeneration
                },
                collections = new
                {
                    gen0 = GC.CollectionCount(0),
                    gen1 = GC.CollectionCount(1),
                    gen2 = GC.CollectionCount(2)
                },
                settings = new
                {
                    isServerGC = GCSettings.IsServerGC,
                    latencyMode = GCSettings.LatencyMode.ToString()
                }
            };

            return Ok(gcInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GC information");
            return StatusCode(500, new { error = "Failed to get GC information" });
        }
    }

    /// <summary>
    /// ThreadPool 상세 정보
    /// </summary>
    [HttpGet("threadpool")]
    public ActionResult<object> GetThreadPoolInfo()
    {
        if (!_environment.IsEnvironment("LoadTest"))
        {
            return BadRequest(new { error = "ThreadPool monitoring is only available in LoadTest environment" });
        }

        try
        {
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                workerThreads = new
                {
                    available = availableWorkerThreads,
                    inUse = maxWorkerThreads - availableWorkerThreads,
                    max = maxWorkerThreads,
                    min = minWorkerThreads
                },
                completionPortThreads = new
                {
                    available = availableCompletionPortThreads,
                    inUse = maxCompletionPortThreads - availableCompletionPortThreads,
                    max = maxCompletionPortThreads,
                    min = minCompletionPortThreads
                },
                pendingWorkItems = ThreadPool.PendingWorkItemCount,
                totalThreads = availableWorkerThreads + availableCompletionPortThreads,
                utilization = new
                {
                    workerThreadsPercent = Math.Round(((double)(maxWorkerThreads - availableWorkerThreads) / maxWorkerThreads) * 100, 2),
                    completionPortThreadsPercent = Math.Round(((double)(maxCompletionPortThreads - availableCompletionPortThreads) / maxCompletionPortThreads) * 100, 2)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ThreadPool information");
            return StatusCode(500, new { error = "Failed to get ThreadPool information" });
        }
    }

    private ActionResult ServiceUnavailable(object value)
    {
        return StatusCode(503, value);
    }
}