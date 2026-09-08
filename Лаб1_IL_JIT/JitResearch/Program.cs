using BenchmarkDotNet.Running;

namespace JitResearch;

/// <summary>
/// Лабораторная работа 1. Задания 4–5: исследование JIT-компиляции.
///
/// Обычный запуск (dotnet run) выполняет ручные замеры Stopwatch —
/// они быстрые и работают в любой конфигурации.
/// Запуск с ключом --bench (и обязательно в Release: dotnet run -c Release -- --bench)
/// дополнительно прогоняет формальный бенчмарк через BenchmarkDotNet.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Лабораторная работа 1: исследование JIT-компиляции ===");

        ColdStartDemo.RunDemo();
        JitVisualization.RunDemo();

        // GetMethod(nameof(RunDemo)) находит метод, а не Main — Main() без
        // модификатора доступа приватный, и GetMethod без BindingFlags.NonPublic
        // такие методы не видит (возвращает null).
        JitVisualization.InspectMethod(typeof(JitVisualization).GetMethod(nameof(JitVisualization.RunDemo))!);
        JitVisualization.CompareJitModes();

        if (args.Contains("--bench"))
        {
            Console.WriteLine();
            Console.WriteLine("=== Задание 4 (доп.): формальный бенчмарк BenchmarkDotNet ===");
            BenchmarkRunner.Run<ArraySumBenchmarks>();
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Подсказка: для формального бенчмарка BenchmarkDotNet запустите");
            Console.WriteLine("  dotnet run -c Release -- --bench");
        }
    }
}
