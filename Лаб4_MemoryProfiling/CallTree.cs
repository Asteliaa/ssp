// Лабораторная работа 4. Call Tree: код из методички (изменения по заданию помечены "ЗАДАНИЕ")
// и профилирование собственного кода (класс OwnCodeProfiler в конце файла).
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization; // ЗАДАНИЕ: для DateTime.ParseExact
using System.Collections; // для собственного кода (ArrayList)
namespace CallTreeExample
{
    class Program
    {
        private static List<string> _logs = new List<string>();
        private static Random _rnd = new Random();
        // Счетчики вызовов для построения Call Tree
        private static Dictionary<string, int> _callCounts = new Dictionary<string, int>();
        private static Dictionary<string, long> _callTimes = new Dictionary<string, long>();
        private static Stopwatch _globalSw = new Stopwatch();
        static void Main()
        {
            _globalSw.Start();
            GenerateData(10_000);
            var result = ProcessLogs();
            _globalSw.Stop();
            Console.WriteLine($"Найдено аномалий: {result.Count}, Общее время: {_globalSw.ElapsedMilliseconds} мс\n");
            // Выводим Call Tree
            PrintCallTree();
            // ЗАДАНИЕ: профилирование собственного небольшого кода
            OwnCodeProfiler.Run();
            Console.ReadKey();
        }
        private static void GenerateData(int count)
        {
            var sw = Stopwatch.StartNew();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                sb.Append($"USER{_rnd.Next(1, 100)}: ");
                sb.Append($"{DateTime.Now.AddMinutes(-_rnd.Next(0, 10000)):yyyy-MM-dd HH:mm:ss.fff} | ");
                sb.Append(_rnd.Next(0, 4) switch
                {
                    0 => "INFO | ",
                    1 => "WARNING | ",
                    2 => "ERROR | ",
                    _ => "DEBUG | "
                });
                for (int j = 0; j < 50; j++)
                    sb.Append(chars[_rnd.Next(chars.Length)]);
                sb.Append($" | Code: {_rnd.Next(100, 999)}");
                _logs.Add(sb.ToString());
            }
            for (int i = 0; i < 100; i++)
                _logs.Add($"ANOMALY: {Guid.NewGuid()} | CRITICAL | Ошибка");
            sw.Stop();
            AddCall("GenerateData", sw.ElapsedMilliseconds);
        }
        private static List<string> ProcessLogs()
        {
            var sw = Stopwatch.StartNew();
            var anomalies = new List<string>();
            // Уровень 1: Фильтрация
            var filterSw = Stopwatch.StartNew();
            var filtered = new List<string>();
            foreach (var log in _logs)
            {
                if (log.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    log.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    log.IndexOf("CRITICAL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(log);
                }
            }
            filterSw.Stop();
            AddCall("FilterLogs (IndexOf)", filterSw.ElapsedMilliseconds);
            // Уровень 2: Анализ аномалий (САМЫЙ ГОРЯЧИЙ)
            var anomalySw = Stopwatch.StartNew();
            foreach (var log in filtered)
            {
                if (IsAnomaly(log))
                    anomalies.Add(log);
            }
            anomalySw.Stop();
            AddCall("IsAnomaly (total with children)", anomalySw.ElapsedMilliseconds);
            sw.Stop();
            AddCall("ProcessLogs", sw.ElapsedMilliseconds);
            return anomalies;
        }
        private static bool IsAnomaly(string log)
        {
            var methodSw = Stopwatch.StartNew();
            bool result = false;
            // Шаг 1: Проверка длины (быстро)
            if (log.Length < 100)
            {
                methodSw.Stop();
                AddCall("IsAnomaly.CheckLength", methodSw.ElapsedMilliseconds);
                return false;
            }
            // Шаг 2: String.Split (средняя тяжесть)
            var splitSw = Stopwatch.StartNew();
            var parts = log.Split('|');
            splitSw.Stop();
            AddCall("IsAnomaly.String.Split", splitSw.ElapsedMilliseconds);
            if (parts.Length < 3)
            {
                methodSw.Stop();
                AddCall("IsAnomaly", methodSw.ElapsedMilliseconds);
                return false;
            }
            // Шаг 3: DateTime.TryParse (тяжелый)
            // ЗАДАНИЕ: вместо DateTime.TryParse используется DateTime.ParseExact с фиксированным форматом.
            // Было: bool dateOk = DateTime.TryParse(parts[1]?.Trim(), out var logDate);
            // Дата находится в parts[0] после "USERxx: ", в parts[1] записан уровень (INFO, ERROR ...).
            var dateSw = Stopwatch.StartNew();
            bool dateOk;
            DateTime logDate;
            try
            {
                logDate = DateTime.ParseExact(parts[0].Substring(parts[0].IndexOf(':') + 1).Trim(),
                    "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                dateOk = true;
            }
            catch (FormatException)
            {
                logDate = DateTime.MinValue;
                dateOk = false;
            }
            dateSw.Stop();
            AddCall("IsAnomaly.DateTime.ParseExact", dateSw.ElapsedMilliseconds);
            if (!dateOk)
            {
                methodSw.Stop();
                AddCall("IsAnomaly", methodSw.ElapsedMilliseconds);
                return false;
            }
            // Шаг 4: Regex (САМЫЙ ТЯЖЕЛЫЙ!)
            var regexSw = Stopwatch.StartNew();
            var codeMatch = Regex.Match(parts.Length > 3 ? parts[3] : "", @"Code:\s*(\d+)");
            var code = codeMatch.Success && int.TryParse(codeMatch.Groups[1].Value, out int c) ? c : 0;
            regexSw.Stop();
            AddCall("IsAnomaly.Regex.Match", regexSw.ElapsedMilliseconds);
            // Шаг 5: Проверки (легкие)
            var checkSw = Stopwatch.StartNew();
            var hasAnomaly = log.Contains("ANOMALY", StringComparison.OrdinalIgnoreCase);
            var isRecent = logDate > DateTime.Now.AddDays(-1);
            result = (hasAnomaly && isRecent) || (code > 500 && isRecent) || (hasAnomaly && code > 500);
            checkSw.Stop();
            AddCall("IsAnomaly.Checks", checkSw.ElapsedMilliseconds);
            methodSw.Stop();
            AddCall("IsAnomaly (own time)", methodSw.ElapsedMilliseconds);
            return result;
        }
        private static void AddCall(string methodName, long timeMs)
        {
            if (!_callCounts.ContainsKey(methodName))
            {
                _callCounts[methodName] = 0;
                _callTimes[methodName] = 0;
            }
            _callCounts[methodName]++;
            _callTimes[methodName] += timeMs;
        }
        private static void PrintCallTree()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                       CALL TREE (CPU TIME)                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝\n");
            long totalTime = _globalSw.ElapsedMilliseconds;
            // Корень
            PrintNode("Main", totalTime, totalTime, 0);
            // Первый уровень
            PrintNode("  ├── GenerateData", _callTimes["GenerateData"], totalTime, 1);
            PrintNode("  └── ProcessLogs", _callTimes["ProcessLogs"], totalTime, 1);
            // Второй уровень (дети ProcessLogs)
            PrintNode("      ├── FilterLogs (IndexOf)", _callTimes["FilterLogs (IndexOf)"], _callTimes["ProcessLogs"], 2);
            PrintNode("      └── IsAnomaly (total with children)", _callTimes["IsAnomaly (total with children)"], _callTimes["ProcessLogs"], 2);
            // Третий уровень (дети IsAnomaly)
            long isAnomalyTotal = _callTimes["IsAnomaly (total with children)"];
            PrintNode("          ├── IsAnomaly.String.Split", _callTimes["IsAnomaly.String.Split"], isAnomalyTotal, 3);
            PrintNode("          ├── IsAnomaly.DateTime.ParseExact", _callTimes["IsAnomaly.DateTime.ParseExact"], isAnomalyTotal, 3); // ЗАДАНИЕ: ParseExact
            PrintNode("          ├── IsAnomaly.Regex.Match", _callTimes["IsAnomaly.Regex.Match"], isAnomalyTotal, 3); // ЗАДАНИЕ: строка раскомментирована
            PrintNode("          ├── IsAnomaly.Checks", _callTimes["IsAnomaly.Checks"], isAnomalyTotal, 3);
            PrintNode("          └── IsAnomaly (own time)", _callTimes["IsAnomaly (own time)"], isAnomalyTotal, 3);
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  🔥 Горячий путь (Hot Path):                                     ║");
            Console.WriteLine("║  Main -> ProcessLogs -> IsAnomaly -> Regex.Match                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"\n📊 Статистика вызовов:");
            foreach (var kvp in _callCounts)
            {
                if (kvp.Key.Contains("Regex"))
                {
                    Console.WriteLine($"   🔥 {kvp.Key}: {kvp.Value} вызовов, {_callTimes[kvp.Key]} мс");
                }
                else
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value} вызовов, {_callTimes[kvp.Key]} мс");
                }
            }
        }
        private static void PrintNode(string name, long time, long parentTime, int level)
        {
            double percent = (double)time / parentTime * 100;
            string percentStr = percent.ToString("F1");
            // Создаем визуальную гистограмму
            int barLength = (int)(percent / 2);
            string bar = new string('█', Math.Min(barLength, 50));
            // Подсветка горячих точек (Regex)
            string indicator = name.Contains("Regex") ? " 🔥🔥🔥" :
                              name.Contains("IsAnomaly (total") ? " 🔥🔥" :
                              name.Contains("Split") || name.Contains("ParseExact") ? " 🔥" : ""; // ЗАДАНИЕ: ParseExact
            Console.WriteLine($"{name.PadRight(35)} {time,5} мс  ({percentStr,5}%)  {bar}{indicator}");
        }
    }

// Профилирование собственного небольшого кода по схеме Call Tree из методички:
// счетчики вызовов и времени (AddCall) и вывод дерева (PrintNode).
internal static class OwnCodeProfiler
{
    private static readonly Dictionary<string, int> _callCounts = new();
    private static readonly Dictionary<string, double> _callTimes = new();

    internal static void Run()
    {
        _callCounts.Clear();
        _callTimes.Clear();
        var total = Stopwatch.StartNew();
        ProcessNumbers(1_000_000);
        ParseDates(10_000);
        total.Stop();

        double totalTime = total.Elapsed.TotalMilliseconds;
        Console.WriteLine("\n=== Call Tree собственного кода ===");
        PrintNode("Main", totalTime, totalTime);
        PrintNode("  ├── ProcessNumbers", _callTimes["ProcessNumbers"], totalTime);
        PrintNode("  │   ├── FillArrayList (boxing)", _callTimes["FillArrayList (boxing)"], _callTimes["ProcessNumbers"]);
        PrintNode("  │   ├── SumArrayList (unboxing)", _callTimes["SumArrayList (unboxing)"], _callTimes["ProcessNumbers"]);
        PrintNode("  │   ├── FillListInt", _callTimes["FillListInt"], _callTimes["ProcessNumbers"]);
        PrintNode("  │   └── SumListInt", _callTimes["SumListInt"], _callTimes["ProcessNumbers"]);
        PrintNode("  └── ParseDates", _callTimes["ParseDates"], totalTime);
        PrintNode("      ├── DateTime.TryParse", _callTimes["DateTime.TryParse"], _callTimes["ParseDates"]);
        PrintNode("      └── DateTime.ParseExact", _callTimes["DateTime.ParseExact"], _callTimes["ParseDates"]);
        Console.WriteLine("\nСтатистика вызовов:");
        foreach (var kvp in _callCounts)
            Console.WriteLine($"   {kvp.Key}: {kvp.Value} вызовов, {_callTimes[kvp.Key]:F3} мс");
    }

    // Код из задания 3: ArrayList с упаковкой и List<int> без нее.
    private static void ProcessNumbers(int count)
    {
        var sw = Stopwatch.StartNew();

        var step = Stopwatch.StartNew();
        var arrayList = new ArrayList();
        for (int i = 0; i < count; i++)
            arrayList.Add(i);
        AddCall("FillArrayList (boxing)", step.Elapsed.TotalMilliseconds);

        step.Restart();
        long sum1 = 0;
        foreach (object item in arrayList)
            sum1 += (int)item;
        AddCall("SumArrayList (unboxing)", step.Elapsed.TotalMilliseconds);

        step.Restart();
        var list = new List<int>();
        for (int i = 0; i < count; i++)
            list.Add(i);
        AddCall("FillListInt", step.Elapsed.TotalMilliseconds);

        step.Restart();
        long sum2 = 0;
        foreach (int item in list)
            sum2 += item;
        AddCall("SumListInt", step.Elapsed.TotalMilliseconds);

        if (sum1 != sum2) throw new InvalidOperationException("Суммы не совпадают");
        AddCall("ProcessNumbers", sw.Elapsed.TotalMilliseconds);
    }

    // Сравнение DateTime.TryParse и DateTime.ParseExact с фиксированным форматом.
    private static void ParseDates(int count)
    {
        var sw = Stopwatch.StartNew();
        var rnd = new Random(42);
        var dates = new List<string>();
        for (int i = 0; i < count; i++)
            dates.Add($"{DateTime.Now.AddMinutes(-rnd.Next(0, 10000)):yyyy-MM-dd HH:mm:ss.fff}");

        foreach (var text in dates)
        {
            var step = Stopwatch.StartNew();
            DateTime.TryParse(text, out _);
            AddCall("DateTime.TryParse", step.Elapsed.TotalMilliseconds);

            step.Restart();
            DateTime.ParseExact(text, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            AddCall("DateTime.ParseExact", step.Elapsed.TotalMilliseconds);
        }
        AddCall("ParseDates", sw.Elapsed.TotalMilliseconds);
    }

    private static void AddCall(string methodName, double timeMs)
    {
        if (!_callCounts.ContainsKey(methodName))
        {
            _callCounts[methodName] = 0;
            _callTimes[methodName] = 0;
        }
        _callCounts[methodName]++;
        _callTimes[methodName] += timeMs;
    }

    private static void PrintNode(string name, double time, double parentTime)
    {
        double percent = time / parentTime * 100;
        int barLength = (int)(percent / 4);
        string bar = new string('█', Math.Min(barLength, 25));
        Console.WriteLine($"{name.PadRight(38)} {time,9:F3} мс  ({percent,5:F1}%)  {bar}");
    }
}

}
