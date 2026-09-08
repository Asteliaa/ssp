using IlAnalysis;
using Xunit;

namespace IlJitLab1.Tests;

public class Task1BasicMethodsTests
{
    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(0, 5, 0)]
    [InlineData(-4, 3, -12)]
    [InlineData(-4, -3, 12)]
    [InlineData(1, 1, 1)]
    [InlineData(int.MinValue, 0, 0)]
    public void Multiply_ReturnsProduct(int a, int b, int expected)
    {
        Assert.Equal(expected, Task1_BasicMethods.Multiply(a, b));
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-5, 5, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(-3, -4, -7)]
    public void SimpleMethod_ReturnsSum(int a, int b, int expected)
    {
        Assert.Equal(expected, Task1_BasicMethods.SimpleMethod(a, b));
    }

    [Theory]
    [InlineData(0, 0)]      // цикл не выполняется ни разу
    [InlineData(1, 1)]
    [InlineData(5, 15)]     // 1+2+3+4+5
    [InlineData(10, 55)]
    [InlineData(-3, 0)]     // отрицательная граница — тело цикла не выполняется
    public void MethodWithLoop_SumsFromOneToN(int n, int expected)
    {
        Assert.Equal(expected, Task1_BasicMethods.MethodWithLoop(n));
    }

    [Theory]
    [InlineData(5, "Positive")]
    [InlineData(1, "Positive")]
    [InlineData(-5, "Negative")]
    [InlineData(-1, "Negative")]
    [InlineData(0, "Zero")]
    public void MethodWithCondition_ClassifiesSign(int value, string expected)
    {
        Assert.Equal(expected, Task1_BasicMethods.MethodWithCondition(value));
    }

    // Задание 1 явно требует получить и проанализировать IL-код —
    // проверяем, что размеры IL и глубина стека соответствуют тому,
    // что задокументировано и объяснено в отчёте (Multiply/SimpleMethod
    // отличаются одним байтом: mul (0x5A) против add (0x58)).
    [Fact]
    public void Multiply_IlBody_HasExpectedShape()
    {
        var method = typeof(Task1_BasicMethods).GetMethod(nameof(Task1_BasicMethods.Multiply))!;
        var body = method.GetMethodBody();

        Assert.NotNull(body);
        byte[] il = body!.GetILAsByteArray()!;

        Assert.Equal(4, il.Length);
        Assert.Equal(0x02, il[0]); // ldarg.0
        Assert.Equal(0x03, il[1]); // ldarg.1
        Assert.Equal(0x5A, il[2]); // mul
        Assert.Equal(0x2A, il[3]); // ret
    }

    [Fact]
    public void SimpleMethod_IlBody_UsesAddInsteadOfMul()
    {
        var method = typeof(Task1_BasicMethods).GetMethod(nameof(Task1_BasicMethods.SimpleMethod))!;
        byte[] il = method.GetMethodBody()!.GetILAsByteArray()!;

        Assert.Equal(4, il.Length);
        Assert.Equal(0x58, il[2]); // add, вместо mul (0x5A) у Multiply
    }
}
