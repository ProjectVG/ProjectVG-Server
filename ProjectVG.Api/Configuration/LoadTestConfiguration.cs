using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace ProjectVG.Api.Configuration;

/// <summary>
/// 부하테스트 환경을 위한 성능 최적화 설정을 제공합니다.
/// </summary>
public static class LoadTestConfiguration
{
    /// <summary>
    /// 부하테스트 환경에서 Kestrel 서버의 성능을 최적화합니다.
    /// </summary>
    /// <param name="options">Kestrel 서버 옵션</param>
    public static void ConfigureKestrelForLoadTest(KestrelServerOptions options)
    {
        // 연결 한도 설정 (부하테스트용 고성능)
        options.Limits.MaxConcurrentConnections = 1000;
        options.Limits.MaxConcurrentUpgradedConnections = 1000;
        
        // 요청 크기 및 타임아웃 최적화
        options.Limits.MaxRequestBodySize = 10_000_000; // 10MB
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
        
        // HTTP/2 최적화
        options.Limits.Http2.MaxStreamsPerConnection = 100;
        options.Limits.Http2.HeaderTableSize = 4096;
        options.Limits.Http2.MaxFrameSize = 16384;
        
        // 성능 로깅
        Console.WriteLine($"LoadTest Kestrel Configuration Applied:");
        Console.WriteLine($"- MaxConcurrentConnections: {options.Limits.MaxConcurrentConnections}");
        Console.WriteLine($"- MaxRequestBodySize: {options.Limits.MaxRequestBodySize} bytes");
    }

    /// <summary>
    /// 부하테스트 환경에서 ThreadPool을 최적화합니다.
    /// </summary>
    public static void ConfigureThreadPoolForLoadTest()
    {
        // ThreadPool 최소 스레드 수 설정 (부하테스트용)
        ThreadPool.SetMinThreads(100, 100);
        
        // ThreadPool 상태 로깅
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
        
        Console.WriteLine($"ThreadPool Configuration:");
        Console.WriteLine($"- Min Worker Threads: {minWorkerThreads}");
        Console.WriteLine($"- Min Completion Port Threads: {minCompletionPortThreads}");
        Console.WriteLine($"- Max Worker Threads: {maxWorkerThreads}");
        Console.WriteLine($"- Max Completion Port Threads: {maxCompletionPortThreads}");
    }
}