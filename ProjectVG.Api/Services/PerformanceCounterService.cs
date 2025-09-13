using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace ProjectVG.Api.Services;

public class PerformanceCounterService : IDisposable
{
    private readonly ILogger<PerformanceCounterService> _logger;
    private readonly Timer _metricsTimer;
    private readonly ConcurrentDictionary<string, object> _lastMetrics;
    private readonly Process _currentProcess;
    private bool _disposed = false;

    public PerformanceCounterService(ILogger<PerformanceCounterService> logger)
    {
        _logger = logger;
        _lastMetrics = new ConcurrentDictionary<string, object>();
        _currentProcess = Process.GetCurrentProcess();
        
        // 5초마다 메트릭 수집
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        _logger.LogInformation("PerformanceCounterService initialized for LoadTest environment");
    }

    public PerformanceMetrics GetCurrentMetrics()
    {
        var metrics = new PerformanceMetrics
        {
            Timestamp = DateTime.UtcNow,
            
            // Process 메트릭
            ProcessId = _currentProcess.Id,
            WorkingSetMemoryMB = _currentProcess.WorkingSet64 / 1024 / 1024,
            PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024 / 1024,
            VirtualMemoryMB = _currentProcess.VirtualMemorySize64 / 1024 / 1024,
            CpuUsagePercent = GetCpuUsage(),
            
            // ThreadPool 메트릭
            ThreadPoolWorkerThreads = GetWorkerThreadCount(),
            ThreadPoolCompletionPortThreads = GetCompletionPortThreadCount(),
            ThreadPoolPendingWorkItems = ThreadPool.PendingWorkItemCount,
            
            // GC 메트릭
            GCGen0Collections = GC.CollectionCount(0),
            GCGen1Collections = GC.CollectionCount(1),
            GCGen2Collections = GC.CollectionCount(2),
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false),
            TotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
            
            // 시스템 메트릭
            AvailableThreads = GetAvailableThreads(),
            SystemCpuCount = Environment.ProcessorCount,
            MachineName = Environment.MachineName
        };

        return metrics;
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            var metrics = GetCurrentMetrics();
            
            // 중요 메트릭만 로깅 (5초마다)
            if (metrics.ThreadPoolPendingWorkItems > 0 || metrics.CpuUsagePercent > 50)
            {
                _logger.LogWarning("High load detected - CPU: {CpuUsage}%, Pending Work: {PendingWork}, Memory: {Memory}MB",
                    metrics.CpuUsagePercent, 
                    metrics.ThreadPoolPendingWorkItems, 
                    metrics.WorkingSetMemoryMB);
            }
            
            // 최신 메트릭 캐시
            _lastMetrics.AddOrUpdate("LastUpdate", metrics.Timestamp, (k, v) => metrics.Timestamp);
            _lastMetrics.AddOrUpdate("WorkingMemory", metrics.WorkingSetMemoryMB, (k, v) => metrics.WorkingSetMemoryMB);
            _lastMetrics.AddOrUpdate("CpuUsage", metrics.CpuUsagePercent, (k, v) => metrics.CpuUsagePercent);
            _lastMetrics.AddOrUpdate("ThreadPoolPending", metrics.ThreadPoolPendingWorkItems, (k, v) => metrics.ThreadPoolPendingWorkItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            return _currentProcess.TotalProcessorTime.TotalMilliseconds;
        }
        catch
        {
            return 0;
        }
    }

    private int GetWorkerThreadCount()
    {
        ThreadPool.GetAvailableThreads(out int workerThreads, out _);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out _);
        return maxWorkerThreads - workerThreads;
    }

    private int GetCompletionPortThreadCount()
    {
        ThreadPool.GetAvailableThreads(out _, out int completionPortThreads);
        ThreadPool.GetMaxThreads(out _, out int maxCompletionPortThreads);
        return maxCompletionPortThreads - completionPortThreads;
    }

    private int GetAvailableThreads()
    {
        ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
        return workerThreads + completionPortThreads;
    }

    public Dictionary<string, object> GetQuickMetrics()
    {
        return new Dictionary<string, object>(_lastMetrics);
    }

    public string GetDotNetCountersCommand()
    {
        return $"dotnet-counters monitor --process-id {_currentProcess.Id} " +
               "Microsoft.AspNetCore.Hosting " +
               "System.Runtime " +
               "Microsoft.AspNetCore.Http.Connections";
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _metricsTimer?.Dispose();
        _currentProcess?.Dispose();
        _disposed = true;
        
        _logger.LogInformation("PerformanceCounterService disposed");
    }
}

public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    
    // Process Metrics
    public int ProcessId { get; set; }
    public long WorkingSetMemoryMB { get; set; }
    public long PrivateMemoryMB { get; set; }
    public long VirtualMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
    
    // ThreadPool Metrics
    public int ThreadPoolWorkerThreads { get; set; }
    public int ThreadPoolCompletionPortThreads { get; set; }
    public long ThreadPoolPendingWorkItems { get; set; }
    
    // GC Metrics
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }
    public long TotalAllocatedBytes { get; set; }
    public long TotalMemoryMB { get; set; }
    
    // System Metrics
    public int AvailableThreads { get; set; }
    public int SystemCpuCount { get; set; }
    public string MachineName { get; set; } = string.Empty;
}