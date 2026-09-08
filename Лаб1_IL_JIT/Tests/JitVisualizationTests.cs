using JitResearch;
using Xunit;

namespace IlJitLab1.Tests;

public class JitVisualizationTests
{
    private static string CaptureOutput(Action action)
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        return writer.ToString();
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, 5, 0)]
    [InlineData(-3, -4, -7)]
    [InlineData(int.MaxValue, 0, int.MaxValue)]
    public void Calculator_Add_ReturnsSum(int a, int b, int expected)
    {
        var calc = new JitVisualization.Calculator();
        Assert.Equal(expected, calc.Add(a, b));
    }

    [Fact]
    public void Calculator_Add_IsOverridable_BecauseItIsVirtual()
    {
        // Задание 5 опирается на то, что Add помечен virtual — таблица
        // виртуальных методов и есть причина, по которой такой вызов
        // не встраивается (inline) JIT-ом. Проверяем это через рефлексию,
        // а не только по документации.
        var method = typeof(JitVisualization.Calculator).GetMethod(nameof(JitVisualization.Calculator.Add))!;
        Assert.True(method.IsVirtual);
    }

    [Fact]
    public void InspectMethod_DoesNotThrow_AndReportsMethodName()
    {
        var method = typeof(JitVisualization).GetMethod(nameof(JitVisualization.RunDemo))!;
        string output = CaptureOutput(() => JitVisualization.InspectMethod(method));

        Assert.Contains("RunDemo", output);
        Assert.Contains("Размер IL", output);
    }

    [Fact]
    public void InspectMethod_DoesNotThrow_ForStaticNonPublicMethod()
    {
        // Регрессионный тест на исправленный баг: раньше метод искали
        // через typeof(Program).GetMethod(nameof(Main)) без BindingFlags,
        // из-за чего для приватного Main() возвращался null и падало
        // NullReferenceException. Проверяем, что для методов, найденных
        // с явными BindingFlags, InspectMethod отрабатывает корректно.
        var method = typeof(ColdStartDemo).GetMethod(nameof(ColdStartDemo.SumArrayCold),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        Assert.NotNull(method);
        var exception = Record.Exception(() => CaptureOutput(() => JitVisualization.InspectMethod(method)));
        Assert.Null(exception);
    }

    [Fact]
    public void CompareJitModes_DoesNotThrow_AndReportsSetting()
    {
        string output = CaptureOutput(JitVisualization.CompareJitModes);
        Assert.Contains("TieredCompilation", output);
    }

    [Fact]
    public void RunDemo_DoesNotThrow_AndReportsAllThreeCallStyles()
    {
        string output = CaptureOutput(JitVisualization.RunDemo);

        Assert.Contains("Прямой вызов", output);
        Assert.Contains("Через делегат", output);
        Assert.Contains("Виртуальный метод", output);
    }
}
