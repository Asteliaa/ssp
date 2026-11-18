// Лабораторная работа 4. Поиск утечек памяти и профилирование производительности в C#.
// Весь код первой части в одном файле:
//   ЧАСТЬ 1 – базовый код методички (дословно, без изменений);
//   ЧАСТЬ 2 – задания 1–4 (фрагменты из заданий дословно + замеры, которых требует задание);
//   ЧАСТЬ 3 – окно программы вместо консоли (вывод Console попадает в окно).
// Call Tree – в файле CallTree.cs.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MemoryLab;
using MemoryLab.Tasks;

// ============================================================================
// ЧАСТЬ 1. Базовый код для лабораторной работы (дословно из методички)
// ============================================================================
// Класс для демонстрации статической утечки
public class MemoryLeakExample
{
    private static List<byte[]> _staticList = new List<byte[]>();
    public static void CreateMemoryLeak()
    {
        for (int i = 0; i < 1000; i++)
            _staticList.Add(new byte[1024 * 10]); // 10KB
    }
    public static void ClearMemoryLeak()
    {
        _staticList.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
// Класс для демонстрации событийной утечки
public class EventLeakExample
{
    public event EventHandler BigEvent;
    private List<EventHandler> _handlers = new List<EventHandler>();
    public void SubscribeLeak()
    {
        EventHandler handler = (sender, e) => 
        {
            var data = new byte[1024 * 100];
        };
        BigEvent += handler;
        _handlers.Add(handler);
    }
    public void UnsubscribeAll()
    {
        foreach (var handler in _handlers)
            BigEvent -= handler;
        _handlers.Clear();
    }
}
// Класс для мониторинга памяти
public class MemoryMonitor : IDisposable
{
    private bool _disposed;
    private List<byte[]> _allocatedMemory = new List<byte[]>();
    public void AllocateMemory(int sizeInMB)
    {
        _allocatedMemory.Add(new byte[sizeInMB * 1024 * 1024]);
    }
    public void AllocateLOHObjects(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var largeObject = new byte[90000 + i];
        }
    }
    public void SimulateBoxing(int count = 1000000)
    {
        var list = new List<object>();
        for (int i = 0; i < count; i++)
            list.Add(i);
    }
    public void PrintMemoryInfo(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine($"\n=== {message} ===");
        Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
        using (Process process = Process.GetCurrentProcess())
            Console.WriteLine($"Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
    }
    public void Cleanup()
    {
        _allocatedMemory.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            Cleanup();
            _disposed = true;
        }
    }
}
// Демонстрация статических ссылок
public class StaticReferenceDemo
{
    private static List<int> _staticList = new List<int>(); // ОДНА для всех
    private List<int> _instanceList = new List<int>();      // Своя для каждого
    public void AddToStatic(int value) => _staticList.Add(value);
    public void AddToInstance(int value) => _instanceList.Add(value);
    public void PrintLists(string name)
    {
        Console.WriteLine($"{name}: Статический [{string.Join(",", _staticList)}], " +
                         $"Экземплярный [{string.Join(",", _instanceList)}]");
    }
}
// Главная программа
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Лабораторная работа: Управление памятью ===\n");
        using (var monitor = new MemoryMonitor())
        {
            // 1. Демонстрация статических ссылок
            Console.WriteLine("1. Статические ссылки:");
            var obj1 = new StaticReferenceDemo();
            var obj2 = new StaticReferenceDemo();
            obj1.AddToStatic(100);
            obj1.AddToInstance(1);
            obj2.AddToStatic(200);
            obj2.AddToInstance(2);
            obj1.PrintLists("Объект 1");
            obj2.PrintLists("Объект 2");
            Console.WriteLine("✅ Статический список ОБЩИЙ для всех объектов!\n");
            // 2. Тест памяти и LOH
            Console.WriteLine("2. Выделение памяти и LOH:");
            monitor.AllocateMemory(10);
            monitor.AllocateLOHObjects(100);
            monitor.PrintMemoryInfo("После выделения");
            // 3. Тест утечек
            Console.WriteLine("\n3. Тест утечек:");
            MemoryLeakExample.CreateMemoryLeak();
            var eventExample = new EventLeakExample();
            for (int i = 0; i < 100; i++)
                eventExample.SubscribeLeak();
            monitor.PrintMemoryInfo("После утечек");
            eventExample.UnsubscribeAll();
            // 4. Тест Boxing
            Console.WriteLine("\n4. Тест упаковки:");
            var sw = Stopwatch.StartNew();
            monitor.SimulateBoxing(1000000);
            sw.Stop();
            Console.WriteLine($"С упаковкой: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            var optimizedList = new List<int>();
            for (int i = 0; i < 1000000; i++)
                optimizedList.Add(i);
            sw.Stop();
            Console.WriteLine($"Без упаковки: {sw.ElapsedMilliseconds}ms");
            // 5. Очистка
            Console.WriteLine("\n5. После очистки:");
            MemoryLeakExample.ClearMemoryLeak();
            monitor.Cleanup();
            monitor.PrintMemoryInfo("Очищено");
            // 6. Поколения объектов
            Console.WriteLine("\n6. Поколения объектов:");
            var obj = new object();
            Console.WriteLine($"Начальное поколение: {GC.GetGeneration(obj)}");
            GC.Collect();
            Console.WriteLine($"После GC: {GC.GetGeneration(obj)}");
        }
        Console.WriteLine("\nНажмите любую клавишу...");
        Console.ReadKey();
    }
}

// ============================================================================
// ЧАСТЬ 2. Задания 1–4
// ============================================================================
namespace MemoryLab.Tasks
{
public static class LabTasks
{
    // Пауза для снимка в Memory Profiler (задается окном программы).
    public static Action<string> Checkpoint = message => { };

    static void PrintKB(string title, long before)
    {
        long now = GC.GetTotalMemory(false);
        Console.WriteLine($"{title}: {now / 1024} KB (изменение {(now - before) / 1024:+0;-0;0} KB)");
    }

    // Задание 1.1 Статическая утечка
    public static void Task1_1()
    {
        Console.WriteLine("=== Задание 1.1. Статическая утечка ===");
        using (var monitor = new MemoryMonitor())
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetTotalMemory(true);
            monitor.PrintMemoryInfo("До создания объектов");

            // Класс со статическим списком: 1000 объектов по 10KB
            MemoryLeakExample.CreateMemoryLeak();
            monitor.PrintMemoryInfo("После добавления 1000 объектов по 10KB");
            PrintKB("Total Memory", before);

            // После GC.Collect() память не освобождается
            GC.Collect();
            GC.WaitForPendingFinalizers();
            monitor.PrintMemoryInfo("После GC.Collect()");
            PrintKB("Total Memory", before);
            Console.WriteLine("Память не освободилась: _staticList (static) продолжает ссылаться на массивы.");
            Checkpoint("Статическая утечка: 1000 массивов по 10KB в _staticList");

            MemoryLeakExample.ClearMemoryLeak();
            GC.GetTotalMemory(true); // дожидаемся завершения сборки перед замером
            monitor.PrintMemoryInfo("После ClearMemoryLeak()");
            PrintKB("Total Memory", before);
        }
    }

    // Задание 1.2 Событийная утечка
    public static void Task1_2()
    {
        Console.WriteLine("=== Задание 1.2. Событийная утечка ===");
        using (var monitor = new MemoryMonitor())
        {
            var eventExample = new EventLeakExample();
            monitor.PrintMemoryInfo("До подписки");
            long before = GC.GetTotalMemory(true);

            // Подписываемся 100 раз на событие
            for (int i = 0; i < 100; i++)
                eventExample.SubscribeLeak();
            long subscribed = GC.GetTotalMemory(true);
            monitor.PrintMemoryInfo("После 100 подписок и GC.Collect()");
            Console.WriteLine($"Рост памяти после GC: {subscribed - before} байт " +
                              "(список вызовов BigEvent и список _handlers на 100 элементов)");
            Checkpoint("Событийная утечка: 100 обработчиков удерживаются BigEvent и _handlers");

            // Отписываемся и показываем освобождение
            long beforeUnsubscribe = GC.GetTotalMemory(true);
            eventExample.UnsubscribeAll();
            long unsubscribed = GC.GetTotalMemory(true);
            monitor.PrintMemoryInfo("После UnsubscribeAll() и GC.Collect()");
            Console.WriteLine($"Освобождено после отписки: {beforeUnsubscribe - unsubscribed} байт");
            GC.KeepAlive(eventExample);
        }
    }

    // Задание 2.1 Создание объектов в LOH
    public static void Task2_1()
    {
        Console.WriteLine("=== Задание 2.1. Создание объектов в LOH ===");
        // Создайте объекты разных размеров
        var small = new byte[80_000];   // 80KB - не в LOH
        var large = new byte[90_000];   // 90KB - в LOH
        // Определите поколение
        Console.WriteLine($"small: Gen{GC.GetGeneration(small)}");
        Console.WriteLine($"large: Gen{GC.GetGeneration(large)}");
        Console.WriteLine("large сразу в Gen2: объекты LOH логически относятся ко второму поколению.");
    }

    // Задание 2.2 Мониторинг LOH
    public static void Task2_2()
    {
        Console.WriteLine("=== Задание 2.2. Мониторинг LOH ===");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        long before = GC.GetTotalMemory(true);
        Console.WriteLine($"До создания: {before / 1024} KB");

        // Создайте 100 объектов по 100KB
        var objects = new List<byte[]>();
        for (int i = 0; i < 100; i++)
            objects.Add(new byte[100 * 1024]);
        long after = GC.GetTotalMemory(false);
        Console.WriteLine($"После создания 100 объектов по 100KB: {after / 1024} KB (+{(after - before) / 1024} KB)");

        // Вызовите GC.Collect(2) и снова измерьте
        GC.Collect(2);
        long afterGc = GC.GetTotalMemory(false);
        Console.WriteLine($"После GC.Collect(2), ссылки сохранены: {afterGc / 1024} KB");
        PrintLoh();
        Checkpoint("100 объектов по 100KB в LOH");

        objects.Clear();   // удаляем ссылки на массивы
        objects = null;
        GC.Collect(2);
        long released = GC.GetTotalMemory(false);
        Console.WriteLine($"После удаления ссылок и GC.Collect(2): {released / 1024} KB (-{(afterGc - released) / 1024} KB)");
        PrintLoh();
    }

    static void PrintLoh()
    {
        GCGenerationInfo loh = GC.GetGCMemoryInfo().GenerationInfo[3];
        Console.WriteLine($"  LOH: {loh.SizeAfterBytes / 1024} KB, фрагментация {loh.FragmentationAfterBytes / 1024} KB");
    }

    // Задание 3.1 Сравнение производительности
    public static void Task3_1()
    {
        Console.WriteLine("=== Задание 3.1. Сравнение производительности ===");
        for (int run = 1; run <= 3; run++)
        {
            GC.Collect();
            long memory = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            // С упаковкой
            var boxed = new List<object>();
            for (int i = 0; i < 1000000; i++)
                boxed.Add(i);
            sw.Stop();
            long boxedMemory = GC.GetTotalMemory(true) - memory;
            long boxedTime = sw.ElapsedMilliseconds;
            GC.KeepAlive(boxed);
            boxed = null;

            GC.Collect();
            memory = GC.GetTotalMemory(true);
            sw.Restart();
            // Без упаковки
            var unboxed = new List<int>();
            for (int i = 0; i < 1000000; i++)
                unboxed.Add(i);
            sw.Stop();
            long unboxedMemory = GC.GetTotalMemory(true) - memory;
            GC.KeepAlive(unboxed);

            Console.WriteLine($"Запуск {run}: List<object> {boxedTime} ms, {boxedMemory / 1024} KB; " +
                              $"List<int> {sw.ElapsedMilliseconds} ms, {unboxedMemory / 1024} KB");
        }
    }

    // Задание 3.2 Оптимизация существующего кода
    public static void Task3_2()
    {
        Console.WriteLine("=== Задание 3.2. Оптимизация существующего кода ===");
        for (int run = 1; run <= 3; run++)
        {
            GC.Collect();
            long memory = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            var unoptimized = new UnoptimizedCode();
            for (int i = 0; i < 1000000; i++)
                unoptimized.Add(i);
            sw.Stop();
            long unoptimizedMemory = GC.GetTotalMemory(true) - memory;
            long unoptimizedTime = sw.ElapsedMilliseconds;
            GC.KeepAlive(unoptimized);
            unoptimized = null;

            GC.Collect();
            memory = GC.GetTotalMemory(true);
            sw.Restart();
            var optimized = new OptimizedCode();
            for (int i = 0; i < 1000000; i++)
                optimized.Add(i);
            sw.Stop();
            long optimizedMemory = GC.GetTotalMemory(true) - memory;
            GC.KeepAlive(optimized);

            Console.WriteLine($"Запуск {run}: UnoptimizedCode (ArrayList) {unoptimizedTime} ms, {unoptimizedMemory / 1024} KB; " +
                              $"OptimizedCode (List<int>) {sw.ElapsedMilliseconds} ms, {optimizedMemory / 1024} KB");
        }
    }

    // Задание 4 Поиск и исправление утечек
    public static void Task4()
    {
        Console.WriteLine("=== Задание 4. Поиск и исправление утечек ===");
        Console.WriteLine("Утечка 1: _globalEvent += OnEvent – статическое событие хранит ссылку на каждый экземпляр.");
        Console.WriteLine("Утечка 2: _data (1 MB) не освобождается, пока экземпляр удерживается событием.");
        Console.WriteLine("Утечка 3: _cache.Add(new MemoryStream(1024)) – статический кэш растет бесконечно, Dispose не вызывается.");
        Console.WriteLine("Утечка 4: OnEvent создает byte[1024 * 100] (LOH) на каждого подписчика при каждом событии.");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long before = GC.GetTotalMemory(true);
        for (int i = 0; i < 100; i++)
            new BuggyCode();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        PrintKB("100 объектов BuggyCode после GC.Collect()", before);
        Checkpoint("BuggyCode: экземпляры удерживаются статическим событием");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        before = GC.GetTotalMemory(true);
        for (int i = 0; i < 100; i++)
            using (var item = new FixedCode()) { }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        PrintKB("100 объектов FixedCode с Dispose после GC.Collect()", before);
        Console.WriteLine("Память BuggyCode остается занятой до завершения процесса – это и есть утечка.");
    }
}

// ❌ НЕОПТИМИЗИРОВАННО
public class UnoptimizedCode
{
    private ArrayList _data = new ArrayList();
    public void Add(int value) => _data.Add(value); // Boxing!
}
// ✅ ОПТИМИЗИРОВАННО
public class OptimizedCode
{
    private List<int> _data = new List<int>();
    public void Add(int value) => _data.Add(value); // Без boxing
}

public class BuggyCode
{
    private static List<IDisposable> _cache = new List<IDisposable>();
    private static event EventHandler _globalEvent;
    private byte[] _data = new byte[1024 * 1024];
    public BuggyCode()
    {
        _globalEvent += OnEvent;      // Утечка 1
        _cache.Add(new MemoryStream(1024)); // Утечка 2
    }
    private void OnEvent(object sender, EventArgs e)
    {
        var temp = new byte[1024 * 100];
    }
}

// Исправленная версия BuggyCode
public class FixedCode : IDisposable
{
    private static event EventHandler _globalEvent;
    private MemoryStream _stream = new MemoryStream(1024); 
    private byte[] _data = new byte[1024 * 1024];
    public FixedCode()
    {
        _globalEvent += OnEvent;
    }
    private void OnEvent(object sender, EventArgs e)
    {
        // временный массив 100KB на каждый вызов не создается
    }
    public void Dispose()
    {
        _globalEvent -= OnEvent; // отписка от статического события
        _stream.Dispose();       // освобождение потока
        _data = null;
    }
}
}

// ============================================================================
// ЧАСТЬ 3. Окно программы (замена консоли)
// ============================================================================
namespace MemoryLab
{

internal sealed record LabItem(string Group, string Title, string Marker, Action Run);

internal sealed class MainForm : Form
{
    internal static readonly LabItem[] Items =
    [
        new("Базовый код методички", "Запустить базовый код", "Б", () => RunMain(typeof(MemoryMonitor), "Program")),
        new("Задание 1. Утечки памяти", "1.1 Статическая утечка", "1.1", LabTasks.Task1_1),
        new("Задание 1. Утечки памяти", "1.2 Событийная утечка", "1.2", LabTasks.Task1_2),
        new("Задание 2. Исследование LOH", "2.1 Создание объектов в LOH", "2.1", LabTasks.Task2_1),
        new("Задание 2. Исследование LOH", "2.2 Мониторинг LOH", "2.2", LabTasks.Task2_2),
        new("Задание 3. Оптимизация Boxing", "3.1 Сравнение производительности", "3.1", LabTasks.Task3_1),
        new("Задание 3. Оптимизация Boxing", "3.2 Оптимизация кода", "3.2", LabTasks.Task3_2),
        new("Задание 4. Поиск утечек", "4 BuggyCode и исправление", "4", LabTasks.Task4),
    ];

    private readonly MemoryChart _chart = new() { Dock = DockStyle.Fill };
    private readonly Label _managed = StatLabel();
    private readonly Label _workingSet = StatLabel();
    private readonly Label _loh = StatLabel();
    private readonly Label _collections = StatLabel();
    private readonly TextBox _log = new();
    private readonly CheckBox _pause = new();
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly List<Button> _buttons = [];
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 250 };
    private volatile bool _pauseEnabled;
    private string _pendingMarker;

    public MainForm()
    {
        Text = "Лабораторная работа 4 – Утечки памяти и профилирование";
        Font = new Font("Segoe UI", 9.5f);
        Rectangle screen = Screen.PrimaryScreen.WorkingArea;
        ClientSize = new Size(Math.Min(S(1240), screen.Width - S(40)), Math.Min(S(780), screen.Height - S(60)));
        MinimumSize = new Size(S(980), S(620));
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(243, 244, 247);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(S(8)) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(280)));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildCommands(), 0, 0);
        root.Controls.Add(BuildWorkspace(), 1, 0);

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        statusStrip.Items.Add(new ToolStripStatusLabel($"PID {Environment.ProcessId} · .NET {Environment.Version} · {IntPtr.Size * 8}-бит"));
        Controls.Add(root);
        Controls.Add(statusStrip);

        // Весь вывод Console из кода методички попадает в журнал окна.
        Console.SetOut(new LogWriter(this));
        LabTasks.Checkpoint = ShowCheckpoint;

        _status.Text = "Выберите задание слева.";
        _timer.Tick += (_, _) => UpdateStats();
        _timer.Start();
        UpdateStats();
    }

    private Control BuildCommands()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = Color.White, Padding = new Padding(S(10), S(8), S(10), S(8)), Margin = new Padding(0, 0, S(8), 0)
        };
        string group = null;
        foreach (LabItem item in Items)
        {
            if (item.Group != group)
            {
                group = item.Group;
                panel.Controls.Add(new Label
                {
                    Text = group, AutoSize = false, Width = S(240), Height = S(24), TextAlign = ContentAlignment.BottomLeft,
                    Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(40, 60, 110),
                    Margin = new Padding(0, panel.Controls.Count == 0 ? 0 : S(8), 0, S(2))
                });
            }
            panel.Controls.Add(CommandButton(item.Title, () => RunAsync([item])));
        }

        panel.Controls.Add(new Label
        {
            Text = "Call Tree (консоль)", AutoSize = false, Width = S(240), Height = S(24), TextAlign = ContentAlignment.BottomLeft,
            Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(40, 60, 110), Margin = new Padding(0, S(8), 0, S(2))
        });
        // Call Tree из CallTree.cs запускается в отдельном консольном окне.
        Button callTree = CommandButton("Открыть Call Tree", () =>
        {
            System.Diagnostics.Process.Start(Environment.ProcessPath, "--calltree");
            return Task.CompletedTask;
        });
        callTree.Tag = "always";
        panel.Controls.Add(callTree);
        panel.Controls.Add(new Label { AutoSize = false, Width = S(240), Height = 1, BackColor = Color.Gainsboro, Margin = new Padding(0, S(12), 0, S(8)) });
        Button all = CommandButton("Выполнить задания 1–4", () => RunAsync(Items.Where(i => i.Group.StartsWith("Задание")).ToList()));
        all.BackColor = Color.FromArgb(48, 92, 180);
        all.ForeColor = Color.White;
        panel.Controls.Add(all);
        Button clear = CommandButton("Очистить журнал", () => { ClearLog(); return Task.CompletedTask; });
        clear.Tag = "always";
        panel.Controls.Add(clear);

        _pause.Text = "Пауза для снимка в Memory Profiler";
        _pause.AutoSize = false;
        _pause.Size = new Size(S(240), S(44));
        _pause.Margin = new Padding(0, S(10), 0, 0);
        _pause.CheckedChanged += (_, _) => _pauseEnabled = _pause.Checked;
        panel.Controls.Add(_pause);
        return panel;
    }

    private Button CommandButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text, Width = S(240), Height = S(31), TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(236, 240, 248), Margin = new Padding(0, S(2), 0, S(2)),
            Padding = new Padding(S(6), 0, 0, 0), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 225);
        button.Click += async (_, _) => await action();
        _buttons.Add(button);
        return button;
    }

    private Control BuildWorkspace()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, S(190)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, S(8)) };
        var stats = new TableLayoutPanel { Dock = DockStyle.Right, Width = S(240), RowCount = 4, Padding = new Padding(S(8), S(10), S(8), S(10)) };
        for (int i = 0; i < 4; i++) stats.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        stats.Controls.AddRange([_managed, _workingSet, _loh, _collections]);
        top.Controls.Add(_chart);
        top.Controls.Add(stats);

        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Both;
        _log.WordWrap = false;
        _log.BackColor = Color.FromArgb(250, 250, 252);
        _log.BorderStyle = BorderStyle.None;
        _log.Font = new Font("Consolas", 9.5f);

        var logPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(S(8), S(4), S(8), S(8)), Margin = Padding.Empty };
        logPanel.Controls.Add(_log);
        logPanel.Controls.Add(new Label
        {
            Text = "Вывод программы", Dock = DockStyle.Top, Height = S(24),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 60, 110)
        });

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(logPanel, 0, 1);
        return layout;
    }

    private static Label StatLabel() => new()
    {
        Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(30, 30, 40),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private async Task RunAsync(IReadOnlyList<LabItem> items)
    {
        SetBusy(true);
        try
        {
            foreach (LabItem item in items)
            {
                _status.Text = "Выполняется: " + item.Title;
                _pendingMarker = item.Marker;
                UpdateStats();
                await Task.Run(() =>
                {
                    item.Run();
                    Console.WriteLine();
                });
            }
            _status.Text = items.Count == 1 ? "Готово: " + items[0].Title : "Задания 1–4 выполнены.";
        }
        catch (Exception ex)
        {
            Console.WriteLine("ОШИБКА: " + ex.Message);
            _status.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // Исходный Main вызывается через Reflection, текст методички не меняется.
    private static void RunMain(Type anchor, string typeName)
    {
        MethodInfo main = anchor.Assembly.GetType(typeName, throwOnError: true)
            .GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        try
        {
            main.Invoke(null, main.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            // Console.ReadKey() в оконном приложении без консоли недоступен.
            Console.WriteLine("(Console.ReadKey() пропущен: приложение работает без консоли)");
        }
    }

    private void SetBusy(bool busy)
    {
        foreach (Button button in _buttons)
            if (button.Tag is not "always") button.Enabled = !busy;
        UseWaitCursor = busy;
        // Пока выполняется задание, окно не опрашивает память, чтобы не влиять на замеры.
        _timer.Enabled = !busy;
        if (!busy) UpdateStats();
    }

    private void ClearLog(bool chart = true)
    {
        _log.Clear();
        if (chart) _chart.Clear();
    }

    internal void AppendLog(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(() => _log.AppendText(text));
        else _log.AppendText(text);
    }

    private void ShowCheckpoint(string message)
    {
        if (!_pauseEnabled) return;
        Invoke(() => MessageBox.Show(this,
            $"{message}.\n\nPID процесса: {Environment.ProcessId}.\nСделайте снимок памяти в профилировщике и нажмите OK.",
            "Снимок памяти", MessageBoxButtons.OK, MessageBoxIcon.Information));
    }

    private void UpdateStats()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        long managed = GC.GetTotalMemory(false);
        GCGenerationInfo[] generations = GC.GetGCMemoryInfo().GenerationInfo.ToArray();
        long loh = generations.Length > 3 ? generations[3].SizeAfterBytes : 0;
        _managed.Text = $"Управляемая куча\n{ToMB(managed):F1} MB";
        _workingSet.Text = $"Working Set\n{ToMB(process.WorkingSet64):F1} MB";
        _loh.Text = $"LOH после последней GC\n{ToMB(loh):F1} MB";
        _collections.Text = $"Сборки Gen0 / Gen1 / Gen2\n{GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}";
        _chart.AddSample(ToMB(managed), ToMB(process.WorkingSet64), _pendingMarker);
        _pendingMarker = null;
    }

    private int S(int pixels) => (int)Math.Round(pixels * DeviceDpi / 96.0);

    private static double ToMB(long bytes) => bytes / (1024.0 * 1024);

    // --screenshots <папка>: снимки окна после каждого задания для отчета.
    internal async Task CaptureScreenshotsAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        TopMost = true;
        (string File, string[] Markers)[] shots =
        [
            ("01_base.png", ["Б"]), ("02_task1_1.png", ["1.1"]), ("03_task1_2.png", ["1.2"]),
            ("04_task2.png", ["2.1", "2.2"]), ("05_task3.png", ["3.1", "3.2"]), ("06_task4.png", ["4"])
        ];
        foreach (var shot in shots)
        {
            ClearLog(chart: false);
            await Task.Delay(1000);
            await RunAsync(Items.Where(i => shot.Markers.Contains(i.Marker)).ToList());
            await Task.Delay(1500);
            _log.SelectionStart = 0;
            _log.ScrollToCaret();
            Activate();
            Refresh();
            await Task.Delay(300);
            CaptureWindow(Path.Combine(directory, shot.File));
            File.WriteAllText(Path.Combine(directory, Path.ChangeExtension(shot.File, ".txt")), _log.Text);
        }
        Close();
    }

    private void CaptureWindow(string path)
    {
        Rectangle bounds = Bounds;
        if (DwmGetWindowAttribute(Handle, 9, out Rect rect, Marshal.SizeOf<Rect>()) == 0)
            bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        bitmap.Save(path, ImageFormat.Png);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out Rect value, int size);

    /// <summary>TextWriter, который передает вывод Console в журнал окна.</summary>
    private sealed class LogWriter(MainForm form) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value) => form.AppendLog(value == '\n' ? Environment.NewLine : value == '\r' ? "" : value.ToString());
        public override void Write(string value) => form.AppendLog((value ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
        public override void WriteLine(string value) => Write(value + "\n");
    }
}

/// <summary>График управляемой памяти и Working Set за последние 60 секунд.</summary>
internal sealed class MemoryChart : Control
{
    private const int Capacity = 240;
    private readonly List<(double Managed, double WorkingSet, string Marker)> _points = [];

    public MemoryChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    public void AddSample(double managed, double workingSet, string marker)
    {
        _points.Add((managed, workingSet, marker));
        if (_points.Count > Capacity) _points.RemoveAt(0);
        Invalidate();
    }

    public void Clear()
    {
        _points.Clear();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using var font = new Font("Segoe UI", 8.5f);
        using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        var managedColor = Color.FromArgb(48, 92, 180);
        var workingSetColor = Color.FromArgb(230, 140, 40);
        const string title = "Память процесса за последние 60 с";
        g.DrawString(title, titleFont, Brushes.Black, S(8), S(6));
        float legend = S(8) + g.MeasureString(title, titleFont).Width + S(24);
        legend = DrawLegend(g, font, managedColor, "Управляемая куча", legend);
        DrawLegend(g, font, workingSetColor, "Working Set", legend);

        var area = new Rectangle(S(58), S(32), Width - S(72), Height - S(48));
        if (area.Width < 20 || area.Height < 20) return;
        double max = _points.Count == 0 ? 64 : _points.Max(p => Math.Max(p.Managed, p.WorkingSet));
        double step = NiceStep(max / 4);
        double top = Math.Max(step * 4, Math.Ceiling(max / step) * step);

        using var gridPen = new Pen(Color.FromArgb(232, 234, 240));
        for (int i = 0; i <= 4; i++)
        {
            float y = area.Bottom - (float)(area.Height * i / 4.0);
            g.DrawLine(gridPen, area.Left, y, area.Right, y);
            string label = $"{top * i / 4:0} MB";
            SizeF size = g.MeasureString(label, font);
            g.DrawString(label, font, Brushes.DimGray, area.Left - size.Width - 4, y - size.Height / 2);
        }
        if (_points.Count < 2) return;

        PointF Map(int index, double value) => new(
            area.Left + (float)(area.Width * index / (double)(Capacity - 1)),
            area.Bottom - (float)(area.Height * value / top));

        using var markerPen = new Pen(Color.FromArgb(150, 150, 160)) { DashStyle = DashStyle.Dash };
        float previous = float.MinValue;
        int row = 0;
        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i].Marker is not string marker) continue;
            float x = Map(i, 0).X;
            // Близкие метки подписываются в разных строках, чтобы не накладывались.
            row = x - previous < S(28) ? row + 1 : 0;
            previous = x;
            g.DrawLine(markerPen, x, area.Top, x, area.Bottom);
            g.DrawString(marker, font, Brushes.DimGray, x + 1, area.Top + row * font.GetHeight(g));
        }
        using var wsPen = new Pen(workingSetColor, 1.6f);
        using var managedPen = new Pen(managedColor, 2.4f);
        g.DrawLines(wsPen, _points.Select((p, i) => Map(i, p.WorkingSet)).ToArray());
        g.DrawLines(managedPen, _points.Select((p, i) => Map(i, p.Managed)).ToArray());
    }

    private float DrawLegend(Graphics g, Font font, Color color, string text, float x)
    {
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x, S(12), S(14), S(4));
        g.DrawString(text, font, Brushes.Black, x + S(18), S(6));
        return x + S(18) + g.MeasureString(text, font).Width + S(16);
    }

    private int S(int pixels) => (int)Math.Round(pixels * DeviceDpi / 96.0);

    private static double NiceStep(double raw)
    {
        double power = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(raw, 1))));
        foreach (double factor in new[] { 1, 2, 2.5, 5, 10 })
            if (factor * power >= raw) return factor * power;
        return 10 * power;
    }
}

internal static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool SetStdHandle(int stdHandle, IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint mode, uint flags, IntPtr template);

    [STAThread]
    private static int Main(string[] args)
    {
        // --calltree: Call Tree из файла CallTree.cs в собственном консольном окне.
        if (args.Length == 1 && args[0] == "--calltree")
        {
            try
            {
                AllocConsole();
                // Ввод и вывод привязываются к новой консоли, чтобы работали Console.WriteLine и Console.ReadKey.
                const uint ReadWrite = 0xC0000000, ShareReadWrite = 3, OpenExisting = 3;
                SetStdHandle(-10, CreateFile("CONIN$", ReadWrite, ShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero));
                IntPtr output = CreateFile("CONOUT$", ReadWrite, ShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                SetStdHandle(-11, output);
                SetStdHandle(-12, output);
                Console.OutputEncoding = Encoding.UTF8;
                typeof(CallTreeExample.Program).GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show((ex.InnerException ?? ex).ToString(), "Call Tree: ошибка");
                return 1;
            }
        }

        // --selftest <файл>: задания без окна, вывод Console пишется в файл (для проверки).
        if (args.Length == 2 && args[0] == "--selftest")
        {
            using var writer = new StreamWriter(args[1]) { AutoFlush = true };
            Console.SetOut(writer);
            try
            {
                foreach (LabItem item in MainForm.Items)
                {
                    item.Run();
                    Console.WriteLine();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ОШИБКА: " + ex);
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        var form = new MainForm();
        // --screenshots <папка>: снимки окна после заданий для отчета.
        if (args.Length == 2 && args[0] == "--screenshots")
            form.Shown += async (_, _) => await form.CaptureScreenshotsAsync(args[1]);
        Application.Run(form);
        return 0;
    }
}
}
