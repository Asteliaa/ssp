using System.Collections;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace MemoryLab;

internal static class Program
{
    private static bool _profile;

    internal static void RunProfile() => Main(["--profile"]);

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (LabLauncher.TryRun(args))
            return;
        _profile = args.Contains("--profile");
        Console.WriteLine("=== Лабораторная работа 2: Управление памятью в C# ===");
        Console.WriteLine($".NET {Environment.Version}; {IntPtr.Size * 8}-битный процесс; PID {Environment.ProcessId}");
        Console.WriteLine("Размеры указаны в KiB/MiB (1024 байт / 1024² байт).");
        BasicMemoryTest();
        StaticAndEventLeaks();
        Generations();
        LargeObjects();
        BoxingBenchmark.Run();
        FixedCodeTest();
        Console.WriteLine("\nВсе проверки выполнены успешно. Принудительный GC используется только для опытов.");
        if (args.Length == 0 && !Console.IsInputRedirected)
        {
            Console.WriteLine("Нажмите Enter для завершения.");
            Console.ReadLine();
        }
    }

    private static void BasicMemoryTest()
    {
        Console.WriteLine("\n=== Задания 1–3. Монитор памяти, IDisposable ===");
        using var monitor = new MemoryMonitor();
        monitor.PrintMemoryInfo("До выделения");
        monitor.AllocateMemory(10);
        monitor.PrintMemoryInfo("После выделения 10 MiB");
        monitor.AllocateLOHObjects(100);
        monitor.PrintMemoryInfo("После создания 100 объектов LOH");
        Checkpoint("Монитор удерживает обычные и большие массивы");
        var watch = Stopwatch.StartNew();
        monitor.SimulateBoxing();
        watch.Stop();
        Console.WriteLine($"Тест упаковки: {watch.Elapsed.TotalMilliseconds:F3} ms");
        monitor.Cleanup();
        monitor.PrintMemoryInfo("После Cleanup и полной GC");
        Checkpoint("Коллекция монитора очищена");
        monitor.Dispose();
        monitor.Dispose();
        try
        {
            monitor.AllocateMemory(1);
            throw new InvalidOperationException("Нет защиты от использования после Dispose.");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Проверка: повторный Dispose безопасен, выделение после Dispose запрещено.");
        }
    }

    private static void StaticAndEventLeaks()
    {
        Console.WriteLine("\n=== Задание 4. Статические и событийные утечки ===");
        StaticLeakDemo.CheckMemory();
        Checkpoint("До создания статической утечки");
        var staticReference = StaticLeakDemo.AddData();
        StaticLeakDemo.AddData();
        StaticLeakDemo.CheckMemory();
        FullCollection();
        StaticLeakDemo.CheckMemory();
        Require(staticReference.IsAlive, "Статическая коллекция удерживает массив после GC");
        Checkpoint("Статическая утечка: 2000 массивов остаются достижимыми");
        StaticLeakDemo.Clear();
        FullCollection();
        Require(!staticReference.IsAlive, "После Clear массив из статического списка собран");
        StaticLeakDemo.CheckMemory();
        Checkpoint("Статическая коллекция очищена");

        using var publisher = new EventLeakDemo();
        FullCollection();
        long before = GC.GetTotalMemory(false);
        var references = new List<WeakReference>();
        for (int i = 0; i < 100; i++)
            references.Add(publisher.Subscribe());
        publisher.Raise();
        FullCollection();
        Console.WriteLine($"100 подписок: прирост {ToMiB(GC.GetTotalMemory(false) - before):F3} MiB");
        Require(references.All(reference => reference.IsAlive), "Событие удерживает 100 массивов по 100 KiB");
        Checkpoint("Событие -> делегат -> замыкание -> массив");
        publisher.Unsubscribe();
        FullCollection();
        Require(references.All(reference => !reference.IsAlive), "После отписки все 100 массивов собраны");
        Console.WriteLine($"После отписки: {ToMiB(GC.GetTotalMemory(false)):F3} MiB");
        Checkpoint("После отписки массивы больше не удерживаются");
        GC.KeepAlive(publisher);
    }

    private static void Generations()
    {
        Console.WriteLine("\n=== Задание 4. Поколения ===");
        var value = new object();
        Console.WriteLine($"Новый объект: Gen{GC.GetGeneration(value)}");
        for (int i = 1; i <= 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine($"После полной GC №{i}: Gen{GC.GetGeneration(value)}");
        }
        GC.KeepAlive(value);
    }

    private static void LargeObjects()
    {
        Console.WriteLine("\n=== Задание 5. Large Object Heap ===");
        using var monitor = new MemoryMonitor();
        FullCollection();
        monitor.PrintMemoryInfo("До 100 массивов по 100 KiB");
        monitor.AllocateLOHObjects(100, 100 * 1024);
        monitor.PrintMemoryInfo("После выделения");
        FullCollection();
        monitor.PrintMemoryInfo("После GC: массивы пока удерживаются монитором");
        Checkpoint("100 живых массивов по 100 KiB в LOH");
        monitor.Cleanup();
        monitor.PrintMemoryInfo("После удаления ссылок и GC");
        Checkpoint("100 массивов LOH освобождены: сравните с предыдущим снимком");

        int[] sizes = [10, 50, 80, 90, 200];
        var arrays = new List<byte[]>();
        foreach (int size in sizes)
        {
            var array = new byte[size * 1024];
            arrays.Add(array);
            Console.WriteLine($"Новый byte[{size} KiB]: Gen{GC.GetGeneration(array)}, " +
                              $"ожидаемая куча при стандартном пороге: {(size >= 90 ? "LOH" : "SOH")}");
        }
        FullCollection();
        for (int i = 0; i < arrays.Count; i++)
            Console.WriteLine($"После GC: {sizes[i]} KiB -> Gen{GC.GetGeneration(arrays[i])}");
        GC.KeepAlive(arrays);
        Console.WriteLine("Gen2 не означает обязательно LOH: туда также переходят обычные долгоживущие объекты.");
        Console.WriteLine("LOH собирается с Gen2; компактизация обычно выключена, но может быть запрошена явно.");
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        FullCollection();
        PrintHeapInfo();
    }

    private static void FixedCodeTest()
    {
        Console.WriteLine("\n=== Задание 7. Исправление BuggyCode ===");
        Console.WriteLine("Утечки: static event удерживает this и _data; static cache бесконечно удерживает потоки.");
        Console.WriteLine("Временный массив обработчика не является утечкой без сохраняющей его ссылки.");
        var weak = CreateDisposedSubscriber();
        FixedCode.SubscribeAll();
        FullCollection();
        Require(!weak.IsAlive, "После Dispose подписчик собран");
        Require(FixedCode.SubscriberCount == 0, "После 1000 экземпляров глобальное событие не содержит подписчиков");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedSubscriber()
    {
        using var subscriber = new FixedCode();
        FixedCode.Raise();
        return new WeakReference(subscriber);
    }

    internal static void FullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    }

    internal static double ToMiB(long bytes) => bytes / (1024.0 * 1024);

    internal static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("Проверка не пройдена: " + message);
        Console.WriteLine("Проверка OK: " + message);
    }

    private static void Checkpoint(string message)
    {
        if (!_profile)
            return;
        Console.WriteLine($"\nSNAPSHOT: {message}. PID {Environment.ProcessId}.");
        Console.WriteLine("Сделайте снимок в профилировщике и нажмите Enter.");
        Console.ReadLine();
    }

    internal static void PrintHeapInfo()
    {
        var info = GC.GetGCMemoryInfo();
        // GenerationInfo[3] описывает LOH в статистике последней GC, а не текущие аллокации.
        if (info.GenerationInfo.Length > 3)
        {
            var loh = info.GenerationInfo[3];
            Console.WriteLine($"LOH после последней GC: размер {ToMiB(loh.SizeAfterBytes):F3} MiB, " +
                              $"фрагментация {ToMiB(loh.FragmentationAfterBytes):F3} MiB");
        }
    }
}

public sealed class MemoryMonitor : IDisposable
{
    private readonly List<byte[]> _allocatedMemory = [];
    private bool _disposed;

    public void AllocateMemory(int sizeInMB)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeInMB);
        _allocatedMemory.Add(new byte[checked(sizeInMB * 1024 * 1024)]);
    }

    public void AllocateLOHObjects(int count, int sizeInBytes = 90000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(sizeInBytes, 85000);
        for (int i = 0; i < count; i++)
            _allocatedMemory.Add(new byte[sizeInBytes]);
    }

    public void SimulateBoxing(int count = 1_000_000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        GC.KeepAlive(BoxingBenchmark.ProcessWithBoxing(count));
    }

    public void PrintMemoryInfo(string message = "")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Console.WriteLine($"\n{message}");
        Console.WriteLine($"Поколение монитора: {GC.GetGeneration(this)}; массивов: {_allocatedMemory.Count}");
        Console.WriteLine($"Управляемая память: {Program.ToMiB(GC.GetTotalMemory(false)):F3} MiB");
        Console.WriteLine($"Число GC: Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");
        using var process = Process.GetCurrentProcess();
        Console.WriteLine($"Working Set: {Program.ToMiB(process.WorkingSet64):F3} MiB");
        Program.PrintHeapInfo();
    }

    public void Cleanup()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _allocatedMemory.Clear();
        Program.FullCollection();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _allocatedMemory.Clear();
        _disposed = true;
        // Dispose убирает ссылки; глобальная принудительная GC здесь не нужна.
    }
}

public static class StaticLeakDemo
{
    private static readonly List<byte[]> Data = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WeakReference AddData()
    {
        for (int i = 0; i < 1000; i++)
            Data.Add(new byte[10 * 1024]);
        return new WeakReference(Data[^1]);
    }

    public static void CheckMemory() => Console.WriteLine(
        $"Память: {Program.ToMiB(GC.GetTotalMemory(false)):F3} MiB; " +
        $"массивов в static: {Data.Count}; поколение списка: {GC.GetGeneration(Data)}");

    public static void Clear() => Data.Clear();
}

public sealed class EventLeakDemo : IDisposable
{
    public event EventHandler? BigEvent;
    private readonly List<EventHandler> _handlers = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference Subscribe()
    {
        byte[]? data = null;
        // Массив создается в обработчике и сохраняется в замыкании подписки.
        EventHandler handler = (_, _) =>
        {
            data ??= new byte[100 * 1024];
            data[0]++;
        };
        _handlers.Add(handler);
        BigEvent += handler;
        handler(this, EventArgs.Empty);
        return new WeakReference(data!);
    }

    public void Raise() => BigEvent?.Invoke(this, EventArgs.Empty);

    public void Unsubscribe()
    {
        foreach (var handler in _handlers)
            BigEvent -= handler;
        _handlers.Clear();
    }

    public void Dispose() => Unsubscribe();
}

public static class BoxingBenchmark
{
    private const int Count = 1_000_000;
    private const int Repeats = 5;
    private readonly record struct Measurement(double Milliseconds, long Retained, long Allocated);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static List<object> ProcessWithBoxing(int count = Count)
    {
        var list = new List<object>(count);
        for (int i = 0; i < count; i++)
            list.Add(i);
        return list;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static List<int> ProcessWithoutBoxing(int count = Count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < count; i++)
            list.Add(i);
        return list;
    }

    public static void Run()
    {
        Console.WriteLine("\n=== Задание 6. Boxing Elimination ===");
        Console.WriteLine("1 000 000 элементов, прогрев, медиана 5 запусков; подготовительная GC вне замера времени.");
        Console.WriteLine("List<object>/List<int>: одинаковая начальная Capacity = 1 000 000.");
        Compare("List<object>", () => ProcessWithBoxing(), "List<int>", () => ProcessWithoutBoxing());
        Console.WriteLine("Исходный ArrayList/List<int>: оба растут с нулевой Capacity; значения 0..999 исключают переполнение int.");
        Compare("ArrayList + сумма", () =>
        {
            var data = new Templates.Additional.UnoptimizedCode();
            for (int i = 0; i < Count; i++) data.AddData(i % 1000);
            if (data.GetSum() != 499_500_000) throw new InvalidOperationException("Неверная сумма");
            return data;
        }, "List<int> + сумма", () =>
        {
            var data = new OptimizedCode(0);
            for (int i = 0; i < Count; i++) data.AddData(i % 1000);
            if (data.GetSum() != 499_500_000L) throw new InvalidOperationException("Неверная сумма");
            return data;
        });
        Console.WriteLine("Ускорение зависит от среды; значение 2–4 раза не гарантируется.");
    }

    private static void Compare(string firstName, Func<object> first, string secondName, Func<object> second)
    {
        GC.KeepAlive(first());
        GC.KeepAlive(second());
        var a = new Measurement[Repeats];
        var b = new Measurement[Repeats];
        for (int i = 0; i < Repeats; i++)
        {
            if (i % 2 == 0) { a[i] = Measure(first); b[i] = Measure(second); }
            else { b[i] = Measure(second); a[i] = Measure(first); }
        }
        Console.WriteLine("| Вариант | Время, ms | Живая память, MiB | Выделено, MiB |");
        Console.WriteLine("|---|---:|---:|---:|");
        Print(firstName, a);
        Print(secondName, b);
        Console.WriteLine($"Отношение времени: {Median(a.Select(x => x.Milliseconds)) / Median(b.Select(x => x.Milliseconds)):F2}x");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Measurement Measure(Func<object> action)
    {
        Program.FullCollection();
        long before = GC.GetTotalMemory(true);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        object result = action();
        double milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long retained = GC.GetTotalMemory(true) - before;
        GC.KeepAlive(result);
        return new Measurement(milliseconds, retained, allocated);
    }

    private static double Median(IEnumerable<double> values) => values.Order().ElementAt(Repeats / 2);

    private static void Print(string name, Measurement[] values) => Console.WriteLine(
        $"| {name} | {Median(values.Select(x => x.Milliseconds)):F3} | " +
        $"{Median(values.Select(x => Program.ToMiB(x.Retained))):F3} | " +
        $"{Median(values.Select(x => Program.ToMiB(x.Allocated))):F3} |");
}

public sealed class UnoptimizedCode(int capacity)
{
    private readonly ArrayList _data = new(capacity);
    public void AddData(int value) => _data.Add(value);
    public long GetSum()
    {
        long sum = 0;
        foreach (object item in _data) sum += (int)item;
        return sum;
    }
}

public sealed class OptimizedCode(int capacity)
{
    private readonly List<int> _data = new(capacity);
    public void AddData(int value) => _data.Add(value);
    public long GetSum()
    {
        long sum = 0;
        foreach (int item in _data) sum += item;
        return sum;
    }
}

public sealed class FixedCode : IDisposable
{
    private static event EventHandler? GlobalEvent;
    private readonly MemoryStream _stream = new(1024);
    private byte[]? _data = new byte[1024 * 1024];
    private bool _disposed;
    public static int SubscriberCount => GlobalEvent?.GetInvocationList().Length ?? 0;

    public FixedCode() => GlobalEvent += OnEvent;

    private void OnEvent(object? sender, EventArgs e)
    {
        if (_data is not null) _data[0]++;
    }

    public static void Raise() => GlobalEvent?.Invoke(null, EventArgs.Empty);

    public static void SubscribeAll()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var item = new FixedCode();
            Raise();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        GlobalEvent -= OnEvent;
        _stream.Dispose();
        _data = null;
        _disposed = true;
    }
}
