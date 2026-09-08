using System.Reflection;

namespace IlAnalysis;

/// <summary>
/// Вспомогательный класс для Задания 1: извлечение и вывод IL-кода метода
/// через рефлексию (MethodBody.GetILAsByteArray).
/// </summary>
internal static class IlInspector
{
    public static void AnalyzeIL(string methodName, Delegate method)
    {
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────────┐");
        Console.WriteLine($"│ Анализ: {methodName,-37}│");
        Console.WriteLine("└─────────────────────────────────────────────┘");

        var methodInfo = method.Method;
        var methodBody = methodInfo.GetMethodBody();

        // GetMethodBody() возвращает null в Release-сборке, т.к. JIT
        // вправе полностью выкинуть отладочные метаданные метода.
        if (methodBody == null)
        {
            Console.WriteLine("Тело метода недоступно (возможно, сборка собрана в Release).");
            Console.WriteLine("Пересоберите проект в конфигурации Debug.");
            return;
        }

        byte[]? ilBytes = methodBody.GetILAsByteArray();
        if (ilBytes == null || ilBytes.Length == 0)
        {
            Console.WriteLine("IL-код не получен.");
            return;
        }

        Console.WriteLine($"Размер IL: {ilBytes.Length} байт");
        Console.WriteLine($"Локальные переменные: {methodBody.LocalVariables.Count}");
        Console.WriteLine($"Максимальный размер стека: {methodBody.MaxStackSize}");

        Console.WriteLine();
        Console.WriteLine("IL-инструкции (HEX):");
        for (int i = 0; i < ilBytes.Length; i++)
        {
            Console.Write($"{ilBytes[i]:X2} ");
            if ((i + 1) % 16 == 0) Console.WriteLine();
        }
        Console.WriteLine();
    }
}
