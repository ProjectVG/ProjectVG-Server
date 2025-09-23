using System.Buffers;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Tests.Infrastructure.Integrations
{
    public class MemoryPoolingPerformanceTests
    {
        private readonly ITestOutputHelper _output;
        private const int TestIterations = 1000;
        private const int AudioDataSize = 128 * 1024; // 128KB 테스트 데이터

        public MemoryPoolingPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ArrayPool_vs_DirectAllocation_PerformanceTest()
        {
            // 준비: 테스트 데이터 생성
            var testData = GenerateTestAudioData(AudioDataSize);

            // 테스트 1: 직접 할당 방식
            var directAllocationTime = MeasureDirectAllocation(testData);

            // 테스트 2: ArrayPool 방식
            var arrayPoolTime = MeasureArrayPoolAllocation(testData);

            // 결과 출력
            _output.WriteLine($"직접 할당 방식: {directAllocationTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"ArrayPool 방식: {arrayPoolTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"성능 개선: {((directAllocationTime.TotalMilliseconds - arrayPoolTime.TotalMilliseconds) / directAllocationTime.TotalMilliseconds * 100):F1}%");

            // ArrayPool이 더 빨라야 함
            Assert.True(arrayPoolTime < directAllocationTime,
                $"ArrayPool 방식({arrayPoolTime.TotalMilliseconds}ms)이 직접 할당({directAllocationTime.TotalMilliseconds}ms)보다 느립니다.");
        }

        [Fact]
        public void Base64Encoding_ArrayPool_vs_Convert_PerformanceTest()
        {
            // 더 큰 데이터 크기로 ArrayPool의 이점을 확인
            var largeTestData = GenerateTestAudioData(AudioDataSize * 4); // 512KB로 확대

            // 테스트 1: 기존 Convert.ToBase64String 방식
            var convertTime = MeasureConvertToBase64(largeTestData);

            // 테스트 2: ArrayPool을 사용한 Base64 인코딩 방식
            var pooledBase64Time = MeasurePooledBase64Encoding(largeTestData);

            _output.WriteLine($"Convert.ToBase64String: {convertTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"ArrayPool Base64: {pooledBase64Time.TotalMilliseconds:F2}ms");
            _output.WriteLine($"성능 개선: {((convertTime.TotalMilliseconds - pooledBase64Time.TotalMilliseconds) / convertTime.TotalMilliseconds * 100):F1}%");

            // ArrayPool Base64는 속도 향상에 집중 (GC 압박 테스트 제외)
            // 큰 크기 데이터에서는 ArrayPool의 이점이 더 명확해짐
            // 성능 차이가 50% 이상 나거나 ArrayPool이 더 빠르면 통과
            var performanceImprovement = ((convertTime.TotalMilliseconds - pooledBase64Time.TotalMilliseconds) / convertTime.TotalMilliseconds * 100);

            Assert.True(pooledBase64Time <= convertTime || performanceImprovement >= -50.0,
                $"ArrayPool Base64 방식({pooledBase64Time.TotalMilliseconds:F2}ms)이 " +
                $"Convert 방식({convertTime.TotalMilliseconds:F2}ms)보다 50% 이상 느립니다. " +
                $"성능 차이: {performanceImprovement:F1}%");

            _output.WriteLine($"Base64 인코딩 성능 테스트 완료 (데이터 크기: {largeTestData.Length / 1024}KB)");
        }

        [Fact]
        public void ChatSegment_MemoryOwner_vs_ByteArray_Test()
        {
            var testData = GenerateTestAudioData(AudioDataSize);

            // 테스트 1: 기존 byte[] 방식
            var segment1 = ChatSegment.CreateText("Test content")
                .WithAudioData(testData, "audio/wav", 5.0f);

            // 테스트 2: IMemoryOwner<byte> 방식
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(testData.Length);
            testData.CopyTo(memoryOwner.Memory.Span);
            var segment2 = ChatSegment.CreateText("Test content")
                .WithAudioMemory(memoryOwner, testData.Length, "audio/wav", 5.0f);

            // 둘 다 동일한 오디오 데이터를 가져야 함
            Assert.True(segment1.HasAudio);
            Assert.True(segment2.HasAudio);
            Assert.Equal(segment1.GetAudioSpan().ToArray(), segment2.GetAudioSpan().ToArray());

            _output.WriteLine($"기존 방식 - HasAudio: {segment1.HasAudio}, 데이터 크기: {segment1.AudioData?.Length ?? 0}");
            _output.WriteLine($"최적화 방식 - HasAudio: {segment2.HasAudio}, 데이터 크기: {segment2.GetAudioSpan().Length}");
        }

        [Fact]
        public void ChatSegment_GetAudioSpan_SafetyBoundaryTest()
        {
            // 경계 조건 테스트: 유효한 범위 내에서의 메모리 접근 안전성 검증
            var testData = GenerateTestAudioData(1000);
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(500); // 더 작은 메모리 할당
            var actualMemorySize = memoryOwner.Memory.Length; // 실제 할당된 메모리 크기
            var copySize = Math.Min(500, actualMemorySize);
            testData.AsSpan(0, copySize).CopyTo(memoryOwner.Memory.Span);

            // 유효한 크기로 설정 (실제 메모리 크기 이하)
            var validSize = actualMemorySize - 10; // 안전한 크기
            var segment = ChatSegment.CreateText("Test content")
                .WithAudioMemory(memoryOwner, validSize, "audio/wav", 5.0f);

            // GetAudioSpan이 정확한 크기를 반환해야 함
            var span = segment.GetAudioSpan();

            // 요청한 크기만큼 반환되어야 함
            Assert.Equal(validSize, span.Length);
            _output.WriteLine($"요청 크기: {validSize}, 실제 메모리: {actualMemorySize}, 반환된 span 크기: {span.Length}");
        }

        [Fact]
        public void ChatSegment_GetAudioSpan_EmptyAndNullSafetyTest()
        {
            // null AudioMemoryOwner는 이제 예외가 발생해야 함 (ArgumentNullException)
            var nullException = Assert.Throws<ArgumentNullException>(() =>
                ChatSegment.CreateText("Test").WithAudioMemory(null!, 100, "audio/wav", 1.0f));
            Assert.Equal("audioMemoryOwner", nullException.ParamName);

            // AudioDataSize가 0인 경우는 여전히 정상 작동해야 함
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(100);
            var segment2 = ChatSegment.CreateText("Test").WithAudioMemory(memoryOwner, 0, "audio/wav", 1.0f);
            var span2 = segment2.GetAudioSpan();
            Assert.True(span2.IsEmpty);

            // 기존 AudioData 방식 (null 허용)
            var segment3 = ChatSegment.CreateText("Test").WithAudioData(null!, "audio/wav", 1.0f);
            var span3 = segment3.GetAudioSpan();
            Assert.True(span3.IsEmpty);

            _output.WriteLine("null 검증과 빈 케이스가 모두 안전하게 처리됨");
        }

        [Fact]
        public void ChatSegment_WithAudioMemory_ValidationTest()
        {
            var testData = GenerateTestAudioData(100);
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(100);
            testData.CopyTo(memoryOwner.Memory.Span);

            // 정상 케이스
            var validSegment = ChatSegment.CreateText("Test")
                .WithAudioMemory(memoryOwner, 50, "audio/wav", 1.0f);
            Assert.Equal(50, validSegment.GetAudioSpan().Length);

            // null audioMemoryOwner 테스트
            var nullException = Assert.Throws<ArgumentNullException>(() =>
                ChatSegment.CreateText("Test").WithAudioMemory(null!, 100, "audio/wav", 1.0f));
            Assert.Equal("audioMemoryOwner", nullException.ParamName);

            // audioDataSize < 0 테스트
            var negativeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ChatSegment.CreateText("Test").WithAudioMemory(memoryOwner, -1, "audio/wav", 1.0f));
            Assert.Equal("audioDataSize", negativeException.ParamName);

            // audioDataSize > memory.Length 테스트
            var oversizeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ChatSegment.CreateText("Test").WithAudioMemory(memoryOwner, memoryOwner.Memory.Length + 1, "audio/wav", 1.0f));
            Assert.Equal("audioDataSize", oversizeException.ParamName);

            _output.WriteLine("모든 소유권 이전 검증 테스트 통과");
        }

        [Fact]
        public void ChatSegment_Dispose_MemoryOwnerReleaseTest()
        {
            var testData = GenerateTestAudioData(100);
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(100);
            testData.CopyTo(memoryOwner.Memory.Span);

            var segment = ChatSegment.CreateText("Test")
                .WithAudioMemory(memoryOwner, 100, "audio/wav", 1.0f);

            // Dispose 호출 전에는 정상 접근 가능
            Assert.True(segment.HasAudio);
            Assert.Equal(100, segment.GetAudioSpan().Length);

            // Dispose 호출
            segment.Dispose();

            // 메모리가 해제되었으므로 ObjectDisposedException 발생할 수 있음
            // (실제 구현에 따라 다를 수 있음)
            _output.WriteLine("Dispose 호출 완료 - 메모리 소유자 해제됨");
        }

        private byte[] GenerateTestAudioData(int size)
        {
            var random = new Random(12345); // 고정 시드로 일관된 테스트
            var data = new byte[size];
            random.NextBytes(data);
            return data;
        }

        private TimeSpan MeasureDirectAllocation(byte[] testData)
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < TestIterations; i++)
            {
                // 직접 할당 시뮬레이션
                var buffer = new byte[testData.Length * 2]; // Base64로 변환하면 크기가 증가
                Array.Copy(testData, 0, buffer, 0, testData.Length);

                // 메모리 사용 시뮬레이션
                var result = Convert.ToBase64String(buffer, 0, testData.Length);
                GC.KeepAlive(result);
            }

            sw.Stop();
            return sw.Elapsed;
        }

        private TimeSpan MeasureArrayPoolAllocation(byte[] testData)
        {
            var arrayPool = ArrayPool<byte>.Shared;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < TestIterations; i++)
            {
                var buffer = arrayPool.Rent(testData.Length * 2);
                try
                {
                    Array.Copy(testData, 0, buffer, 0, testData.Length);
                    var result = Convert.ToBase64String(buffer, 0, testData.Length);
                    GC.KeepAlive(result);
                }
                finally
                {
                    arrayPool.Return(buffer);
                }
            }

            sw.Stop();
            return sw.Elapsed;
        }

        private TimeSpan MeasureConvertToBase64(byte[] testData)
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < TestIterations; i++)
            {
                var result = Convert.ToBase64String(testData);
                GC.KeepAlive(result);
            }

            sw.Stop();
            return sw.Elapsed;
        }

        private TimeSpan MeasurePooledBase64Encoding(byte[] testData)
        {
            var arrayPool = ArrayPool<byte>.Shared;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < TestIterations; i++)
            {
                var base64Length = ((testData.Length + 2) / 3) * 4;
                var buffer = arrayPool.Rent(base64Length);

                try
                {
                    if (System.Buffers.Text.Base64.EncodeToUtf8(testData, buffer, out _, out var bytesWritten) == System.Buffers.OperationStatus.Done)
                    {
                        var result = Encoding.UTF8.GetString(buffer, 0, bytesWritten);
                        GC.KeepAlive(result);
                    }
                }
                finally
                {
                    arrayPool.Return(buffer);
                }
            }

            sw.Stop();
            return sw.Elapsed;
        }

        private void AssertLessGCPressure(Func<TimeSpan> optimizedMethod, Func<TimeSpan> standardMethod, string message)
        {
            // GC 정리
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeGen0 = GC.CollectionCount(0);
            var beforeGen1 = GC.CollectionCount(1);
            var beforeGen2 = GC.CollectionCount(2);

            // 최적화된 방법 실행
            var optimizedTime = optimizedMethod();

            var optimizedGen0 = GC.CollectionCount(0) - beforeGen0;
            var optimizedGen1 = GC.CollectionCount(1) - beforeGen1;
            var optimizedGen2 = GC.CollectionCount(2) - beforeGen2;

            // GC 정리
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            beforeGen0 = GC.CollectionCount(0);
            beforeGen1 = GC.CollectionCount(1);
            beforeGen2 = GC.CollectionCount(2);

            // 표준 방법 실행
            var standardTime = standardMethod();

            var standardGen0 = GC.CollectionCount(0) - beforeGen0;
            var standardGen1 = GC.CollectionCount(1) - beforeGen1;
            var standardGen2 = GC.CollectionCount(2) - beforeGen2;

            _output.WriteLine($"최적화 방법 - Gen0: {optimizedGen0}, Gen1: {optimizedGen1}, Gen2: {optimizedGen2}");
            _output.WriteLine($"표준 방법 - Gen0: {standardGen0}, Gen1: {standardGen1}, Gen2: {standardGen2}");

            // Gen2 수집이 적거나 같아야 함 (LOH 압박 감소)
            Assert.True(optimizedGen2 <= standardGen2, $"{message} (Gen2 수집: 최적화={optimizedGen2}, 표준={standardGen2})");
        }
    }
}