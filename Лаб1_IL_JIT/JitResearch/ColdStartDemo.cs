using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JitResearch;

/// <summary>
/// Задание 4.1. Оценка влияния JIT-компиляции на скорость сложения
/// элементов массива из 10000 целых чисел.
///
/// В управляемой среде .NET нельзя буквально выполнить код "без JIT" —
/// любой managed-метод перед первым вызовом обязательно проходит через
/// JIT-компилятор. Поэтому влияние JIT измеряется не как "есть/нет",
/// а как разница между:
///   1) первым ("холодным") вызовом метода, время которого включает
///      компиляцию IL в машинный код;
///   2) вызовом уже скомпилированного ("прогретого") метода —
///      для этого JIT форсируется заранее через RuntimeHelpers.PrepareMethod.
/// Оба метода структурно идентичны и различаются только тем, что один
/// из них принудительно прогрет перед замером.
/// </summary>
internal static class ColdStartDemo
{
    private const int ArraySize = 10000;
    private const int SteadyStateRepeats = 100_000;

    // Два идентичных по телу метода нужны для того, чтобы у каждого
    // был свой, независимый от другого, статус JIT-компиляции.
    // internal (не private) — чтобы тестовый проект мог проверить их
    // корректность напрямую, отдельно от замера времени в RunDemo().
    internal static long SumArrayCold(int[] data)
    {
        long sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    internal static long SumArrayWarm(int[] data)
    {
        long sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    public static void RunDemo()
    {
        Console.WriteLine();
        Console.WriteLine("=== Задание 4.1: влияние JIT-компиляции на суммирование массива (10000 элементов) ===");

        var data = new int[ArraySize];
        for (int i = 0; i < ArraySize; i++)
            data[i] = i + 1;

        var coldMethod = typeof(ColdStartDemo).GetMethod(nameof(SumArrayCold),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var warmMethod = typeof(ColdStartDemo).GetMethod(nameof(SumArrayWarm),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var coldDelegate = (Func<int[], long>)coldMethod.CreateDelegate(typeof(Func<int[], long>));
        var warmDelegate = (Func<int[], long>)warmMethod.CreateDelegate(typeof(Func<int[], long>));

        // Форсируем JIT-компиляцию SumArrayWarm ДО замера: к моменту
        // вызова метод уже полностью скомпилирован в машинный код.
        RuntimeHelpers.PrepareMethod(warmMethod.MethodHandle);

        // Холодный вызов: JIT компилирует метод прямо во время замера.
        var sw = Stopwatch.StartNew();
        long coldResult = coldDelegate(data);
        sw.Stop();
        double coldMicroseconds = sw.Elapsed.TotalMicroseconds;

        // Прогретый вызов: JIT уже отработал заранее.
        sw.Restart();
        long warmResult = warmDelegate(data);
        sw.Stop();
        double warmMicroseconds = sw.Elapsed.TotalMicroseconds;

        Console.WriteLine($"Холодный вызов (SumArrayCold, включает JIT-компиляцию): {coldMicroseconds:F2} мкс, результат = {coldResult}");
        Console.WriteLine($"Прогретый вызов (SumArrayWarm, JIT выполнен заранее):  {warmMicroseconds:F2} мкс, результат = {warmResult}");
        Console.WriteLine($"Оценка накладных расходов JIT-компиляции: {coldMicroseconds - warmMicroseconds:F2} мкс");

        // Устойчивое состояние: после первого ("холодного") вызова
        // SumArrayCold сам становится уже скомпилированным, поэтому
        // среднее время последующих вызовов должно быть близко к
        // прогретому вызову, а не к холодному.
        sw.Restart();
        long steadyStateSum = 0;
        for (int i = 0; i < SteadyStateRepeats; i++)
            steadyStateSum += coldDelegate(data);
        sw.Stop();
        double avgSteadyStateMicroseconds = sw.Elapsed.TotalMicroseconds / SteadyStateRepeats;

        Console.WriteLine($"Среднее время вызова в установившемся режиме ({SteadyStateRepeats:N0} повторов): {avgSteadyStateMicroseconds:F4} мкс");
        Console.WriteLine("Вывод: холодный вызов на 1–2 порядка медленнее установившегося режима — " +
                           "именно эта разница и есть цена JIT-компиляции метода при первом обращении к нему.");
    }
}
