namespace IlAnalysis;

/// <summary>
/// Лабораторная работа 1. Платформа .NET: анализ IL-кода и JIT-компиляции.
/// Точка входа для Заданий 1–3 (анализ IL, генерация методов через ILGenerator
/// и DynamicMethod). Задания 4–5 (JIT-бенчмаркинг) вынесены в проект JitResearch.
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=== Лабораторная работа 1: Анализ IL-кода и JIT-компиляции ===");

        Console.WriteLine();
        Console.WriteLine("################  Задание 1: анализ IL базовых конструкций  ################");
        IlInspector.AnalyzeIL("Multiply(int, int) — произведение двух чисел", (Func<int, int, int>)Task1_BasicMethods.Multiply);
        IlInspector.AnalyzeIL("SimpleMethod — сложение", (Func<int, int, int>)Task1_BasicMethods.SimpleMethod);
        IlInspector.AnalyzeIL("MethodWithLoop — цикл for", (Func<int, int>)Task1_BasicMethods.MethodWithLoop);
        IlInspector.AnalyzeIL("MethodWithCondition — ветвление if/else", (Func<int, string>)Task1_BasicMethods.MethodWithCondition);

        Console.WriteLine();
        Console.WriteLine("################  Задания 2–3: динамическая генерация IL  ################");
        DynamicMethodsDemo.RunDemo();

        Console.WriteLine();
        Console.WriteLine("Готово. Для Заданий 4–5 (исследование JIT-компиляции) запустите проект JitResearch.");
    }
}
