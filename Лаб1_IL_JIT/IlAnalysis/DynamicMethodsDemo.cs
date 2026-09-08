using System.Reflection;
using System.Reflection.Emit;

namespace IlAnalysis;

/// <summary>
/// Задания 2 и 3. Динамическая генерация методов через
/// DynamicMethod + ILGenerator.
///
/// Задание 2.2 и Задание 3.1 фактически используют один и тот же
/// метод-генератор: ILGenerator в .NET получают исключительно через
/// DynamicMethod.GetILGenerator(), поэтому "работа с IL-генератором"
/// и "создание динамического метода" — две стороны одного и того же API,
/// а не разные механизмы.
/// </summary>
internal static class DynamicMethodsDemo
{
    // Задание 2.1: динамический метод для вычисления n! рекурсивно.
    // long вместо int — иначе переполнение уже на 13!.
    public static Func<long, long> CreateFactorialRecursiveMethod()
    {
        var dynamicMethod = new DynamicMethod(
            "DynamicFactorialRecursive",
            typeof(long),
            new[] { typeof(long) },
            typeof(DynamicMethodsDemo).Module);

        ILGenerator il = dynamicMethod.GetILGenerator();
        Label baseCase = il.DefineLabel();

        // if (n <= 1) goto baseCase;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Ble, baseCase);

        // return n * Factorial(n - 1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Sub);
        // Рекурсивный вызов: ссылаемся на сам dynamicMethod ещё до
        // завершения его построения — Reflection.Emit это допускает,
        // т.к. DynamicMethod уже существует как объект MethodInfo.
        il.Emit(OpCodes.Call, dynamicMethod);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        // baseCase: return 1;
        il.MarkLabel(baseCase);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Ret);

        return (Func<long, long>)dynamicMethod.CreateDelegate(typeof(Func<long, long>));
    }

    // Задание 2.2 / Задание 3.1: динамический метод для нахождения
    // максимума из трёх чисел. Реализован через попарное сравнение:
    // сначала max(a, b) сохраняется в локальную переменную,
    // затем она сравнивается с c.
    public static Func<int, int, int, int> CreateMaxOfThreeMethod()
    {
        var dynamicMethod = new DynamicMethod(
            "DynamicMaxOfThree",
            typeof(int),
            new[] { typeof(int), typeof(int), typeof(int) },
            typeof(DynamicMethodsDemo).Module);

        ILGenerator il = dynamicMethod.GetILGenerator();
        il.DeclareLocal(typeof(int)); // local 0: max(a, b)

        Label aGreater = il.DefineLabel();
        Label afterFirstCompare = il.DefineLabel();
        Label localGreater = il.DefineLabel();

        // local0 = (a > b) ? a : b;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Bgt, aGreater);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Br, afterFirstCompare);
        il.MarkLabel(aGreater);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc_0);
        il.MarkLabel(afterFirstCompare);

        // return (local0 > c) ? local0 : c;
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Bgt, localGreater);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ret);
        il.MarkLabel(localGreater);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);

        return (Func<int, int, int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int, int, int>));
    }

    // Задание 3.2: динамический метод для нахождения абсолютной величины
    // вещественного числа. IL не содержит инструкции "abs" — она
    // собирается вручную из сравнения и унарного минуса (Neg).
    public static Func<double, double> CreateAbsValueMethod()
    {
        var dynamicMethod = new DynamicMethod(
            "DynamicAbsValue",
            typeof(double),
            new[] { typeof(double) },
            typeof(DynamicMethodsDemo).Module);

        ILGenerator il = dynamicMethod.GetILGenerator();
        Label nonNegative = il.DefineLabel();

        // if (x >= 0.0) goto nonNegative;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_R8, 0.0);
        il.Emit(OpCodes.Bge, nonNegative);

        // return -x;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Neg);
        il.Emit(OpCodes.Ret);

        // nonNegative: return x;
        il.MarkLabel(nonNegative);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);

        return (Func<double, double>)dynamicMethod.CreateDelegate(typeof(Func<double, double>));
    }

    public static void RunDemo()
    {
        Console.WriteLine();
        Console.WriteLine("=== Задание 2.1: рекурсивный факториал через ILGenerator ===");
        var factorial = CreateFactorialRecursiveMethod();
        for (long n = 0; n <= 10; n++)
            Console.WriteLine($"  {n}! = {factorial(n)}");

        Console.WriteLine();
        Console.WriteLine("=== Задание 2.2 / 3.1: максимум из трёх чисел ===");
        var maxOfThree = CreateMaxOfThreeMethod();
        (int, int, int)[] triples = { (3, 7, 5), (-1, -5, -2), (10, 10, 3), (0, 0, 0) };
        foreach (var (a, b, c) in triples)
            Console.WriteLine($"  max({a}, {b}, {c}) = {maxOfThree(a, b, c)}");

        Console.WriteLine();
        Console.WriteLine("=== Задание 3.2: абсолютная величина вещественного числа ===");
        var absValue = CreateAbsValueMethod();
        double[] samples = { -12.5, 0.0, 3.14, -0.001, 100.0 };
        foreach (var x in samples)
            Console.WriteLine($"  |{x}| = {absValue(x)}");
    }
}
