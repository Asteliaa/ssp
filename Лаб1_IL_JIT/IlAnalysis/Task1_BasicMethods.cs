namespace IlAnalysis;

/// <summary>
/// Задание 1. Анализ IL-кода базовых конструкций языка C#.
/// Каждый метод демонстрирует свою группу IL-инструкций:
/// арифметика, циклы, условные переходы.
/// </summary>
internal static class Task1_BasicMethods
{
    // Задание 1: "напишите простой c# код, например для отыскания
    // произведения двух целых чисел" — базовая арифметика, стековая машина
    // сведёт это к паре Ldarg + Mul + Ret.
    public static int Multiply(int a, int b) => a * b;

    // Пример из методички — простое сложение.
    public static int SimpleMethod(int a, int b) => a + b;

    // Пример из методички — цикл for, порождает Br/Blt и метки.
    public static int MethodWithLoop(int n)
    {
        int sum = 0;
        for (int i = 1; i <= n; i++)
            sum += i;
        return sum;
    }

    // Пример из методички — ветвление if/else if/else, порождает
    // серию инструкций сравнения и условных переходов (Bgt/Blt/Br).
    public static string MethodWithCondition(int value)
    {
        if (value > 0)
            return "Positive";
        else if (value < 0)
            return "Negative";
        else
            return "Zero";
    }
}
