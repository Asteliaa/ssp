using IlAnalysis;
using Xunit;

namespace IlJitLab1.Tests;

public class DynamicMethodsDemoTests
{
    // --- Задание 2.1: рекурсивный факториал через ILGenerator ---

    [Theory]
    [InlineData(0, 1)]   // база рекурсии: 0! = 1
    [InlineData(1, 1)]   // база рекурсии: 1! = 1
    [InlineData(2, 2)]
    [InlineData(3, 6)]
    [InlineData(5, 120)]
    [InlineData(10, 3628800)]
    [InlineData(15, 1307674368000)]  // проверка, что long не переполняется там, где переполнился бы int
    public void Factorial_ReturnsExpectedValue(long n, long expected)
    {
        var factorial = DynamicMethodsDemo.CreateFactorialRecursiveMethod();
        Assert.Equal(expected, factorial(n));
    }

    [Fact]
    public void Factorial_IsRecreatedFresh_EachCallToFactory()
    {
        // Каждый вызов CreateFactorialRecursiveMethod генерирует новый
        // DynamicMethod; оба должны быть независимо корректны.
        var f1 = DynamicMethodsDemo.CreateFactorialRecursiveMethod();
        var f2 = DynamicMethodsDemo.CreateFactorialRecursiveMethod();

        Assert.Equal(720, f1(6));
        Assert.Equal(720, f2(6));
    }

    // --- Задание 2.2 / 3.1: максимум из трёх чисел через ILGenerator ---

    [Theory]
    [InlineData(3, 7, 5, 7)]     // максимум — второй аргумент
    [InlineData(7, 3, 5, 7)]     // максимум — первый аргумент
    [InlineData(3, 5, 7, 7)]     // максимум — третий аргумент
    [InlineData(-1, -5, -2, -1)] // все отрицательные
    [InlineData(10, 10, 3, 10)]  // два равных максимума (ветка a > b ложна на равенстве)
    [InlineData(0, 0, 0, 0)]     // все равны
    [InlineData(5, 5, 5, 5)]
    [InlineData(int.MaxValue, 0, int.MinValue, int.MaxValue)]
    public void MaxOfThree_ReturnsLargestValue(int a, int b, int c, int expected)
    {
        var maxOfThree = DynamicMethodsDemo.CreateMaxOfThreeMethod();
        Assert.Equal(expected, maxOfThree(a, b, c));
    }

    // --- Задание 3.2: абсолютная величина вещественного числа ---

    [Theory]
    [InlineData(-12.5, 12.5)]
    [InlineData(12.5, 12.5)]
    [InlineData(0.0, 0.0)]         // ноль — граничный случай Bge/Neg
    [InlineData(-0.001, 0.001)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(double.MinValue, double.MaxValue)] // MinValue — самое большое по модулю отрицательное
    public void AbsValue_ReturnsNonNegativeMagnitude(double x, double expected)
    {
        var abs = DynamicMethodsDemo.CreateAbsValueMethod();
        Assert.Equal(expected, abs(x));
    }

    [Fact]
    public void AbsValue_OfNaN_IsNaN()
    {
        // NaN не удовлетворяет ни x >= 0, ни x < 0 ни в каком сравнении;
        // условие Bge ложно для NaN, поэтому берётся ветка Neg(NaN) = NaN.
        var abs = DynamicMethodsDemo.CreateAbsValueMethod();
        Assert.True(double.IsNaN(abs(double.NaN)));
    }
}
