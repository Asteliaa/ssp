using JitResearch;
using Xunit;

namespace IlJitLab1.Tests;

// Проверяем корректность самих бенчмарк-методов отдельно от замера
// производительности: BenchmarkDotNet измеряет скорость, но не
// гарантирует, что три реализации возвращают одинаковый результат —
// это должны подтвердить обычные unit-тесты.
public class ArraySumBenchmarksTests
{
    private static ArraySumBenchmarks CreatePrepared(int size)
    {
        var bench = new ArraySumBenchmarks { Size = size };
        bench.Setup();
        return bench;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(10_000)]
    public void AllThreeMethods_AgreeOnResult(int size)
    {
        var bench = CreatePrepared(size);

        long loop = bench.LoopSum();
        long linq = bench.LinqSum();
        long span = bench.SpanSum();

        Assert.Equal(loop, linq);
        Assert.Equal(loop, span);
    }

    [Fact]
    public void LoopSum_OfTenThousandElements_MatchesAnalyticalSum()
    {
        var bench = CreatePrepared(10_000);
        long expected = 10_000L * 10_001L / 2L; // сумма 1..10000, как в Задании 4.1

        Assert.Equal(expected, bench.LoopSum());
    }

    [Fact]
    public void Setup_WithSizeOne_ProducesSingleElementArray()
    {
        var bench = CreatePrepared(1);
        Assert.Equal(1, bench.LoopSum()); // единственный элемент data[0] = 0 + 1 = 1
    }
}
