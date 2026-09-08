using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JitResearch;

/// <summary>
/// Задание 5 (для продвинутых). Профилирование и сравнение
/// производительности разных способов вызова одной и той же операции:
/// прямой вызов, вызов через делегат, вызов виртуального метода.
/// Дополняет и завершает пример JitVisualization.cs из методички —
/// в частности, недописанный блок сравнения с виртуальным методом.
/// </summary>
internal static class JitVisualization
{
    /// <summary>
    /// Выводит информацию о скомпилированном методе: адрес машинного
    /// кода, размер IL, число локальных переменных и признаки метода.
    /// </summary>
    public static void InspectMethod(MethodInfo method)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Инспекция метода: {method.Name} ===");

        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        nint functionPointer = method.MethodHandle.GetFunctionPointer();
        Console.WriteLine($"Адрес метода: 0x{functionPointer:X16}");

        var methodBody = method.GetMethodBody();
        if (methodBody != null)
        {
            Console.WriteLine($"Размер IL: {methodBody.GetILAsByteArray()?.Length ?? 0} байт");
            Console.WriteLine($"Локальные переменные: {methodBody.LocalVariables.Count}");
            Console.WriteLine($"Максимальный стек: {methodBody.MaxStackSize}");
        }

        Console.WriteLine("Атрибуты метода:");
        Console.WriteLine($"  IsVirtual: {method.IsVirtual}");
        Console.WriteLine($"  IsStatic: {method.IsStatic}");
        Console.WriteLine($"  IsAbstract: {method.IsAbstract}");
        Console.WriteLine($"  IsFinal: {method.IsFinal}");
    }

    /// <summary>
    /// Проверяет, включена ли многоуровневая (Tiered) JIT-компиляция
    /// в текущей среде выполнения, и поясняет, как её отключить.
    /// </summary>
    public static void CompareJitModes()
    {
        Console.WriteLine();
        Console.WriteLine("=== Сравнение режимов JIT ===");

        bool tieredEnabled = GetTieredCompilationSetting();
        Console.WriteLine($"TieredCompilation: {(tieredEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
        Console.WriteLine("Для изменения режима добавьте в .csproj:");
        Console.WriteLine("  <TieredCompilation>false</TieredCompilation>");
    }

    private static bool GetTieredCompilationSetting()
    {
        try
        {
            var configProperty = typeof(RuntimeHelpers).GetProperty("IsTieredCompilation");
            if (configProperty != null)
                return (bool)configProperty.GetValue(null)!;
        }
        catch
        {
            // Свойство отсутствует в данной версии рантайма — считаем
            // Tiered Compilation включённой по умолчанию (так и есть
            // в .NET 6+, если явно не отключена в runtimeconfig.json).
        }
        return true;
    }

    // internal, а не private — так тестовый проект может проверить
    // Calculator.Add напрямую, не изменяя видимость на public.
    internal class Calculator
    {
        public virtual int Add(int a, int b) => a + b;
    }

    public static void RunDemo()
    {
        Console.WriteLine();
        Console.WriteLine("=== Задание 5: профилирование производительности вызовов ===");

        const int iterations = 10_000_000;
        var sw = new Stopwatch();

        // Прямой вызов: JIT способен встроить (inline) тело метода
        // прямо в тело цикла — накладных расходов на вызов почти нет.
        sw.Start();
        int sum1 = 0;
        for (int i = 0; i < iterations; i++)
            sum1 += i;
        sw.Stop();
        Console.WriteLine($"Прямой вызов:      {sw.ElapsedMilliseconds} мс (результат {sum1})");

        // Вызов через делегат: адрес метода определяется через
        // объект-делегат — JIT не всегда может встроить такой вызов.
        Func<int, int, int> add = (x, y) => x + y;
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < iterations; i++)
            sum2 = add(sum2, i);
        sw.Stop();
        Console.WriteLine($"Через делегат:     {sw.ElapsedMilliseconds} мс (результат {sum2})");

        // Вызов виртуального метода: JIT обязан обратиться к таблице
        // виртуальных методов (v-table) объекта на каждом вызове,
        // поэтому инлайнинг невозможен, и вызов всегда дороже прямого.
        var calc = new Calculator();
        sw.Restart();
        int sum3 = 0;
        for (int i = 0; i < iterations; i++)
            sum3 = calc.Add(sum3, i);
        sw.Stop();
        Console.WriteLine($"Виртуальный метод: {sw.ElapsedMilliseconds} мс (результат {sum3})");

        Console.WriteLine();
        Console.WriteLine("Вывод: прямой вызов обычно самый быстрый благодаря инлайнингу; " +
                           "вызов через делегат и виртуальный вызов сопоставимы между собой " +
                           "и медленнее прямого, так как оба требуют разыменования указателя " +
                           "на метод во время выполнения вместо подстановки кода на этапе JIT.");
    }
}
