using JitResearch;
using Xunit;

namespace IlJitLab1.Tests;

public class ColdStartDemoTests
{
    [Fact]
    public void SumArrayCold_SumsAllElements()
    {
        int[] data = { 1, 2, 3, 4, 5 };
        Assert.Equal(15, ColdStartDemo.SumArrayCold(data));
    }

    [Fact]
    public void SumArrayWarm_SumsAllElements()
    {
        int[] data = { 1, 2, 3, 4, 5 };
        Assert.Equal(15, ColdStartDemo.SumArrayWarm(data));
    }

    [Fact]
    public void SumArrayCold_And_SumArrayWarm_AgreeOnResult()
    {
        // Оба метода структурно идентичны (см. комментарий в ColdStartDemo.cs) —
        // их результат обязан совпадать для любого входного массива.
        var data = Enumerable.Range(1, 10_000).ToArray();
        Assert.Equal(ColdStartDemo.SumArrayCold(data), ColdStartDemo.SumArrayWarm(data));
    }

    [Fact]
    public void SumArrayCold_OfTenThousandElements_MatchesAnalyticalSum()
    {
        // 1 + 2 + ... + 10000 = 10000 * 10001 / 2, ровно тот массив
        // (i + 1 для i от 0 до 9999), который используется в Задании 4.1.
        var data = Enumerable.Range(0, 10_000).Select(i => i + 1).ToArray();
        long expected = 10_000L * 10_001L / 2L;

        Assert.Equal(expected, ColdStartDemo.SumArrayCold(data));
    }

    [Fact]
    public void SumArrayCold_OfEmptyArray_ReturnsZero()
    {
        Assert.Equal(0L, ColdStartDemo.SumArrayCold(Array.Empty<int>()));
    }

    [Fact]
    public void SumArrayCold_WithNegativeNumbers_SumsCorrectly()
    {
        int[] data = { -5, 10, -3, 8, -100 };
        Assert.Equal(-90, ColdStartDemo.SumArrayCold(data));
    }

    [Fact]
    public void RunDemo_DoesNotThrow()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            var exception = Record.Exception(ColdStartDemo.RunDemo);
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
