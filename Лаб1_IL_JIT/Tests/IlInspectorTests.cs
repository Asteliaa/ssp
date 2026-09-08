using IlAnalysis;
using Xunit;

namespace IlJitLab1.Tests;

public class IlInspectorTests
{
    // AnalyzeIL печатает в Console.Out — перехватываем его, чтобы
    // проверить содержимое вывода, не завязываясь на устройство экрана.
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

    [Fact]
    public void AnalyzeIL_Multiply_ReportsCorrectIlSize()
    {
        string output = CaptureOutput(() =>
            IlInspector.AnalyzeIL("Multiply", (Func<int, int, int>)Task1_BasicMethods.Multiply));

        Assert.Contains("Размер IL: 4 байт", output);
        Assert.Contains("02 03 5A 2A", output);
    }

    [Fact]
    public void AnalyzeIL_MethodWithLoop_ReportsLocalsAndSize()
    {
        string output = CaptureOutput(() =>
            IlInspector.AnalyzeIL("MethodWithLoop", (Func<int, int>)Task1_BasicMethods.MethodWithLoop));

        Assert.Contains("Размер IL: 32 байт", output);
        Assert.Contains("Локальные переменные: 4", output);
    }

    [Fact]
    public void AnalyzeIL_DoesNotThrow_ForAnyDelegate()
    {
        // Проверяем устойчивость на разных сигнатурах методов —
        // AnalyzeIL не должен падать независимо от типа делегата.
        var exception = Record.Exception(() => CaptureOutput(() =>
            IlInspector.AnalyzeIL("MethodWithCondition", (Func<int, string>)Task1_BasicMethods.MethodWithCondition)));

        Assert.Null(exception);
    }
}
