// Лабораторная работа 5. Часть 2. Параллельное программирование в C#
// с использованием TPL, PLINQ и разделения данных
//
// Все демонстрации собраны в одном файле Program.cs.
// Шаблоны кода из методички перенесены без изменений:
//   - Задание 1. Введение в параллельные циклы (namespace ParallelProgramming)
//   - Задание 2. Parallel.ForEach с коллекциями (namespace Task2_ParallelForEach)
//   - Задание 3. PLINQ для анализа данных (namespace Task3_Plinq)
//   - Задание 4. Разделение данных и агрегация (namespace Task4_Partitioning)
//   - Задание 5. Комплексная обработка изображений (namespace Task5_Images)
// Плюс все варианты индивидуальных заданий (namespace Variant1 ... Variant7):
//   - Вариант 1. Обработка больших массивов данных: суммирование, поиск максимума
//   - Вариант 2. Параллельная фильтрация коллекций: PLINQ, пользовательские условия
//   - Вариант 3. Матричные вычисления: умножение, транспонирование
//   - Вариант 4. Обработка изображений: фильтры, преобразования
//   - Вариант 5. Анализ текстовых данных: параллельный поиск, подсчет
//   - Вариант 6. Численные методы: интегрирование, оптимизация
//   - Вариант 7. Сортировка и поиск: параллельные алгоритмы
// В вариантах используются только средства из методички: Parallel.For/ForEach,
// локальные для потока переменные, lock/Interlocked, Partitioner, PLINQ
// (AsParallel, AsOrdered, WithDegreeOfParallelism), CancellationToken, state.Break().
//
// В методичке задания 2-5 лежат в одном namespace LabWork_ParallelProgramming
// с одинаковым классом Program, поэтому для сборки в одном файле каждому
// заданию дан свой namespace. Тела методов не изменены, кроме задания 2:
// string? input с проверкой на null (Nullable включен) и убран финальный
// Console.ReadKey(), т.к. после задания программа возвращается в меню.
//
// Точка входа: _5_2laba.Program (задана через <StartupObject> в .csproj).
// Замеры времени корректно смотреть в Release: dotnet run -c Release

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// =====================================================================
// МЕНЮ ЗАПУСКА
// =====================================================================
namespace _5_2laba
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=== Лабораторная работа 5.2. Параллельное программирование: TPL, PLINQ, разделение данных ===");
                Console.WriteLine($"Логических процессоров: {Environment.ProcessorCount}\n");
                Console.WriteLine("1. Задание 1. Введение в параллельные циклы");
                Console.WriteLine("2. Задание 2. Parallel.ForEach с коллекциями (отмена, локальные переменные)");
                Console.WriteLine("3. Задание 3. PLINQ для анализа данных");
                Console.WriteLine("4. Задание 4. Разделение данных и агрегация");
                Console.WriteLine("5. Задание 5. Комплексная обработка изображений");
                Console.WriteLine("6.  Вариант 1. Обработка больших массивов данных (суммирование, поиск максимума)");
                Console.WriteLine("7.  Вариант 2. Параллельная фильтрация коллекций (PLINQ, пользовательские условия)");
                Console.WriteLine("8.  Вариант 3. Матричные вычисления (умножение, транспонирование)");
                Console.WriteLine("9.  Вариант 4. Обработка изображений (фильтры, преобразования)");
                Console.WriteLine("10. Вариант 5. Анализ текстовых данных (параллельный поиск, подсчет)");
                Console.WriteLine("11. Вариант 6. Численные методы (интегрирование, оптимизация)");
                Console.WriteLine("12. Вариант 7. Сортировка и поиск (параллельные алгоритмы)");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите пункт: ");

                string? choice = Console.ReadLine();
                if (string.IsNullOrEmpty(choice))
                {
                    return;
                }
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        ParallelProgramming.Program.Main(args);
                        break;
                    case "2":
                        Task2_ParallelForEach.Program.Main(args);
                        break;
                    case "3":
                        Task3_Plinq.Program.Main(args);
                        break;
                    case "4":
                        Task4_Partitioning.Program.Main(args);
                        break;
                    case "5":
                        Task5_Images.Program.Main(args);
                        break;
                    case "6":
                        Variant1.Program.Main(args);
                        break;
                    case "7":
                        Variant2.Program.Main(args);
                        break;
                    case "8":
                        Variant3.Program.Main(args);
                        break;
                    case "9":
                        Variant4.Program.Main(args);
                        break;
                    case "10":
                        Variant5.Program.Main(args);
                        break;
                    case "11":
                        Variant6.Program.Main(args);
                        break;
                    case "12":
                        Variant7.Program.Main(args);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Неверный выбор.");
                        break;
                }

                Console.WriteLine();
            }
        }
    }
}

// =====================================================================
// ЗАДАНИЕ 1. ВВЕДЕНИЕ В ПАРАЛЛЕЛЬНЫЕ ЦИКЛЫ – ШАБЛОН ИЗ МЕТОДИЧКИ
// Сравнение последовательного цикла, Parallel.For с lock на каждой
// итерации и Parallel.For с локальными для потока переменными.
// =====================================================================
namespace ParallelProgramming
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 1: ПАРАЛЛЕЛЬНЫЕ ЦИКЛЫ ===\n");

            // Создание большого массива данных
            const int N = 10000000;
            double[] data = new double[N];
            Random rand = new Random();

            // Инициализация данных
            for (int i = 0; i < N; i++)
            {
                data[i] = rand.NextDouble() * 1000;
            }

            // --- Последовательный цикл ---
            Stopwatch sw = Stopwatch.StartNew();
            double sumSeq = 0;
            for (int i = 0; i < N; i++)
            {
                sumSeq += Math.Sqrt(Math.Log(data[i] + 1));
            }
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс, Результат: {sumSeq:F2}");

            // --- Параллельный цикл (Parallel.For) ---
            sw.Restart();
            double sumPar = 0;
            object lockObj = new object();
            Parallel.For(0, N, i =>
            {
                double value = Math.Sqrt(Math.Log(data[i] + 1));
                lock (lockObj)
                {
                    sumPar += value;
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (с блокировкой): {sw.ElapsedMilliseconds} мс, Результат: {sumPar:F2}");

            // --- Параллельный цикл с локальными для потока переменными (оптимизированный) ---
            sw.Restart();
            double sumParLocal = 0;
            Parallel.For(0, N,
                () => 0.0, // Локальный инициализатор
                (i, state, local) => local + Math.Sqrt(Math.Log(data[i] + 1)), // Тело
                local => { lock (lockObj) { sumParLocal += local; } } // Агрегатор
            );
            sw.Stop();
            Console.WriteLine($"Параллельно (локальные переменные): {sw.ElapsedMilliseconds} мс, Результат: {sumParLocal:F2}");

            Console.WriteLine("\n=== Задание 1 Выполнено ===\n");
        }
    }
}

// =====================================================================
// ЗАДАНИЕ 2. PARALLEL.FOREACH С КОЛЛЕКЦИЯМИ – ШАБЛОН ИЗ МЕТОДИЧКИ
// Трансформация, фильтрация с локальными списками, поиск с отменой
// (CancellationToken + state.Break()), сравнение lock и локальных
// переменных, интерактивный поиск. Ввод 'exit' – возврат в меню.
// =====================================================================
namespace Task2_ParallelForEach
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 2: ПАРАЛЛЕЛЬНАЯ ОБРАБОТКА ДАННЫХ ===\n");

            // Генерация данных
            const int N = 10_000_000;
            int[] numbers = new int[N];
            Random rand = new Random();
            for (int i = 0; i < N; i++) numbers[i] = rand.Next(1, 1000);

            // 1. Трансформация (Parallel.For)
            Console.WriteLine("--- Трансформация данных ---");
            double[] transformed = new double[N];
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, N, i => transformed[i] = Math.Sqrt(Math.Pow(numbers[i], 2) + 1));
            sw.Stop();
            Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Пример: {transformed[0]:F2}, {transformed[1000]:F2}, {transformed[5000]:F2}\n");

            // 2. Фильтрация (Parallel.ForEach с локальными переменными)
            Console.WriteLine("--- Фильтрация (>500 и кратные 7) ---");
            List<int> filtered = new List<int>();
            object listLock = new object();
            var ranges = Enumerable.Range(0, N).Select(i => numbers[i]);

            sw.Restart();
            Parallel.ForEach(ranges,
                () => new List<int>(),
                (val, state, local) =>
                {
                    if (val > 500 && val % 7 == 0) local.Add(val);
                    return local;
                },
                local => { lock (listLock) filtered.AddRange(local); }
            );
            sw.Stop();
            Console.WriteLine($"Найдено: {filtered.Count}, Время: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Примеры: {string.Join(", ", filtered.Take(5))}\n");

            // 3. Поиск с отменой
            Console.WriteLine("--- Поиск с отменой ---");
            int[] targets = { 777, 999, 123 };
            bool[] found = new bool[targets.Length];
            int[] positions = new int[targets.Length];
            CancellationTokenSource cts = new CancellationTokenSource();

            sw.Restart();
            try
            {
                Parallel.For(0, N, new ParallelOptions { CancellationToken = cts.Token }, (i, state) =>
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        if (!found[j] && numbers[i] == targets[j])
                        {
                            lock (found)
                            {
                                if (!found[j])
                                {
                                    found[j] = true;
                                    positions[j] = i;
                                    Console.WriteLine($"✅ Найдено {targets[j]} на позиции {i}");
                                    if (found.All(f => f)) { cts.Cancel(); state.Break(); return; }
                                }
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException) { Console.WriteLine("✅ Все найдено!"); }
            sw.Stop();
            Console.WriteLine($"Время поиска: {sw.ElapsedMilliseconds} мс\n");

            // 4. Сравнение производительности
            Console.WriteLine("--- Сравнение: блокировка vs локальные ---");
            long sumLock = 0, sumLocal = 0;
            object lockObj = new object();

            // С блокировкой
            sw.Restart();
            Parallel.For(0, N, i => { lock (lockObj) sumLock += numbers[i] * numbers[i]; });
            sw.Stop();
            Console.WriteLine($"С блокировкой: {sw.ElapsedMilliseconds} мс");

            // С локальными переменными
            sw.Restart();
            Parallel.For(0, N,
                () => 0L,
                (i, state, local) => local + numbers[i] * numbers[i],
                local => { lock (lockObj) sumLocal += local; }
            );
            sw.Stop();
            Console.WriteLine($"С локальными: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Результаты: {sumLock:N0} = {sumLocal:N0}\n");

            // 5. Интерактивный поиск
            Console.WriteLine("=== Интерактивный поиск ===");
            Console.WriteLine("Введите число (или 'exit'):");
            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (input == null || input.ToLower() == "exit") break;
                if (int.TryParse(input, out int search))
                {
                    bool foundVal = false;
                    int pos = -1;
                    Parallel.For(0, N, (i, state) =>
                    {
                        if (numbers[i] == search) { pos = i; foundVal = true; state.Break(); }
                    });
                    Console.WriteLine(foundVal ? $"✅ Найдено на позиции {pos}" : "❌ Не найдено");
                }
            }

            Console.WriteLine("\n=== Задание 2 Выполнено ===");
        }
    }
}

// =====================================================================
// ЗАДАНИЕ 3. PLINQ ДЛЯ АНАЛИЗА ДАННЫХ – ШАБЛОН ИЗ МЕТОДИЧКИ
// Последовательный LINQ, AsParallel(), WithDegreeOfParallelism(4),
// AsOrdered().
// =====================================================================
namespace Task3_Plinq
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 3: PLINQ АНАЛИЗ ДАННЫХ ===\n");

            // Генерация большого набора данных
            const int N = 10000000;
            Random rand = new Random();
            var data = Enumerable.Range(0, N)
                .Select(i => rand.NextDouble() * 1000)
                .ToList();

            // --- Последовательный LINQ ---
            Stopwatch sw = Stopwatch.StartNew();
            var seqResult = data
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"Последовательный LINQ: {sw.ElapsedMilliseconds} мс, Результат: {seqResult:F4}");

            // --- PLINQ (базовый) ---
            sw.Restart();
            var plinqResult = data
                .AsParallel()
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"PLINQ (по умолчанию): {sw.ElapsedMilliseconds} мс, Результат: {plinqResult:F4}");

            // --- PLINQ со степенью параллелизма ---
            sw.Restart();
            var plinqResult2 = data
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"PLINQ (степень=4): {sw.ElapsedMilliseconds} мс, Результат: {plinqResult2:F4}");

            // --- PLINQ с сохранением порядка ---
            sw.Restart();
            var plinqOrdered = data
                .AsParallel()
                .AsOrdered()
                .Where(x => x > 500)
                .Select(x => x * 2)
                .Take(5)
                .ToList();
            sw.Stop();
            Console.WriteLine($"PLINQ (упорядоченный): {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Первые 5 результатов: {string.Join(", ", plinqOrdered)}");

            Console.WriteLine("\n=== Задание 3 Выполнено ===\n");
        }
    }
}

// =====================================================================
// ЗАДАНИЕ 4. РАЗДЕЛЕНИЕ ДАННЫХ И АГРЕГАЦИЯ – ШАБЛОН ИЗ МЕТОДИЧКИ
// Разделение на диапазоны (Parallel.For + Interlocked), блочное
// разделение (Partitioner.Create), локальная для потока агрегация.
// =====================================================================
namespace Task4_Partitioning
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 4: РАЗДЕЛЕНИЕ ДАННЫХ ===\n");

            const int N = 10000000;
            int[] data = new int[N];
            Random rand = new Random();

            for (int i = 0; i < N; i++)
            {
                data[i] = rand.Next(1, 100);
            }

            // --- Разделение на диапазоны (по умолчанию) ---
            Stopwatch sw = Stopwatch.StartNew();
            long sumRange = 0;
            Parallel.For(0, N, i =>
            {
                Interlocked.Add(ref sumRange, data[i]);
            });
            sw.Stop();
            Console.WriteLine($"Разделение на диапазоны: {sw.ElapsedMilliseconds} мс, Сумма: {sumRange}");

            // --- Пользовательское разделение с Partitioner ---
            sw.Restart();
            long sumCustom = 0;
            var partitioner = Partitioner.Create(0, N, 100000);
            Parallel.ForEach(partitioner, range =>
            {
                long localSum = 0;
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    localSum += data[i];
                }
                Interlocked.Add(ref sumCustom, localSum);
            });
            sw.Stop();
            Console.WriteLine($"Пользовательское разделение: {sw.ElapsedMilliseconds} мс, Сумма: {sumCustom}");

            // --- Локальная для потока агрегация ---
            sw.Restart();
            long sumLocal = 0;
            Parallel.For(0, N,
                () => 0L,
                (i, state, local) => local + data[i],
                local => Interlocked.Add(ref sumLocal, local)
            );
            sw.Stop();
            Console.WriteLine($"Локальная агрегация: {sw.ElapsedMilliseconds} мс, Сумма: {sumLocal}");

            Console.WriteLine("\n=== Задание 4 Выполнено ===\n");
        }
    }
}

// =====================================================================
// ЗАДАНИЕ 5. КОМПЛЕКСНАЯ ОБРАБОТКА ИЗОБРАЖЕНИЙ – ШАБЛОН ИЗ МЕТОДИЧКИ
// Изображение 2000x2000 как double[,]: последовательно, Parallel.For
// по строкам, Partitioner по блокам строк, PLINQ.
// =====================================================================
namespace Task5_Images
{
    class Program
    {
        // Симуляция функции обработки пикселя
        static double ProcessPixel(double pixel)
        {
            // Сложное вычисление, имитирующее фильтр изображения
            return Math.Sqrt(Math.Abs(Math.Sin(pixel) * Math.Cos(pixel) * 1000));
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 5: ПАРАЛЛЕЛЬНАЯ ОБРАБОТКА ИЗОБРАЖЕНИЙ ===\n");

            // Симуляция изображения как двумерного массива
            const int width = 2000;
            const int height = 2000;
            double[,] image = new double[height, width];
            double[,] result = new double[height, width];

            Random rand = new Random();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[y, x] = rand.NextDouble();
                }
            }

            // --- Последовательная обработка ---
            Stopwatch sw = Stopwatch.StartNew();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = ProcessPixel(image[y, x]);
                }
            }
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");

            // --- Параллельная обработка (по строкам) ---
            sw.Restart();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = ProcessPixel(image[y, x]);
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (по строкам): {sw.ElapsedMilliseconds} мс");

            // --- Параллельная обработка с разделением ---
            sw.Restart();
            var partitioner = Partitioner.Create(0, height, 100);
            Parallel.ForEach(partitioner, range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        result[y, x] = ProcessPixel(image[y, x]);
                    }
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (с разделением): {sw.ElapsedMilliseconds} мс");

            // --- Обработка с помощью PLINQ ---
            sw.Restart();
            var pixels = Enumerable.Range(0, height * width)
                .AsParallel()
                .Select(i => ProcessPixel(image[i / width, i % width]))
                .ToArray();
            sw.Stop();
            Console.WriteLine($"PLINQ: {sw.ElapsedMilliseconds} мс");

            Console.WriteLine("\n=== Задание 5 Выполнено ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 1.
// ОБРАБОТКА БОЛЬШИХ МАССИВОВ ДАННЫХ: СУММИРОВАНИЕ, ПОИСК МАКСИМУМА
// Последовательно, Parallel.For + Interlocked / lock на каждой итерации,
// Parallel.For с локальными переменными, Partitioner, PLINQ.
// =====================================================================
namespace Variant1
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 1: ОБРАБОТКА БОЛЬШИХ МАССИВОВ ДАННЫХ ===\n");

            // Генерация данных
            const int N = 20_000_000;
            int[] data = new int[N];
            Random rand = new Random();
            for (int i = 0; i < N; i++) data[i] = rand.Next(1, 1_000_000);
            Console.WriteLine($"Сгенерировано {N:N0} чисел в диапазоне [1; 1 000 000)\n");

            object lockObj = new object();

            // 1. Суммирование
            Console.WriteLine("--- Суммирование ---");
            Stopwatch sw = Stopwatch.StartNew();
            long sumSeq = 0;
            for (int i = 0; i < N; i++) sumSeq += data[i];
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:                   {seqTime,6} мс, Сумма: {sumSeq}");

            // Interlocked на каждой итерации
            sw.Restart();
            long sumInterlocked = 0;
            Parallel.For(0, N, i => Interlocked.Add(ref sumInterlocked, data[i]));
            sw.Stop();
            Console.WriteLine($"Parallel.For + Interlocked:        {sw.ElapsedMilliseconds,6} мс, Сумма: {sumInterlocked}");

            // Локальные для потока переменные
            sw.Restart();
            long sumLocal = 0;
            Parallel.For(0, N,
                () => 0L,
                (i, state, local) => local + data[i],
                local => Interlocked.Add(ref sumLocal, local)
            );
            sw.Stop();
            long localTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (локальные):          {localTime,6} мс, Сумма: {sumLocal}");

            // Блочное разделение
            sw.Restart();
            long sumPart = 0;
            Parallel.ForEach(Partitioner.Create(0, N, 100000), range =>
            {
                long localSum = 0;
                for (int i = range.Item1; i < range.Item2; i++) localSum += data[i];
                Interlocked.Add(ref sumPart, localSum);
            });
            sw.Stop();
            Console.WriteLine($"Partitioner (блоки по 100 000):    {sw.ElapsedMilliseconds,6} мс, Сумма: {sumPart}");

            // PLINQ
            sw.Restart();
            long sumPlinq = data.AsParallel().Sum(x => (long)x);
            sw.Stop();
            Console.WriteLine($"PLINQ Sum:                         {sw.ElapsedMilliseconds,6} мс, Сумма: {sumPlinq}");
            Console.WriteLine($"Суммы совпадают: {sumSeq == sumInterlocked && sumSeq == sumLocal && sumSeq == sumPart && sumSeq == sumPlinq}, " +
                              $"ускорение (локальные): {(double)seqTime / Math.Max(localTime, 1):F1} раз\n");

            // 2. Поиск максимума (и его позиции)
            Console.WriteLine("--- Поиск максимума ---");
            sw.Restart();
            int maxSeq = int.MinValue, maxSeqIndex = -1;
            for (int i = 0; i < N; i++)
            {
                if (data[i] > maxSeq) { maxSeq = data[i]; maxSeqIndex = i; }
            }
            sw.Stop();
            seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:                   {seqTime,6} мс, Максимум: {maxSeq} (позиция {maxSeqIndex})");

            // lock на каждой итерации
            sw.Restart();
            int maxLock = int.MinValue;
            Parallel.For(0, N, i => { lock (lockObj) { if (data[i] > maxLock) maxLock = data[i]; } });
            sw.Stop();
            Console.WriteLine($"Parallel.For + lock:               {sw.ElapsedMilliseconds,6} мс, Максимум: {maxLock}");

            // Локальный максимум потока + одна блокировка в конце
            sw.Restart();
            int maxLocal = int.MinValue;
            Parallel.For(0, N,
                () => int.MinValue,
                (i, state, local) => Math.Max(local, data[i]),
                local => { lock (lockObj) maxLocal = Math.Max(maxLocal, local); }
            );
            sw.Stop();
            localTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (локальные):          {localTime,6} мс, Максимум: {maxLocal}");

            // Блочное разделение: максимум и позиция в каждом блоке
            sw.Restart();
            int maxPart = int.MinValue, maxPartIndex = -1;
            Parallel.ForEach(Partitioner.Create(0, N, 100000), range =>
            {
                int localMax = int.MinValue, localIndex = -1;
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (data[i] > localMax) { localMax = data[i]; localIndex = i; }
                }
                lock (lockObj)
                {
                    // При равных значениях берем меньший индекс – как в последовательном поиске
                    if (localMax > maxPart || (localMax == maxPart && localIndex < maxPartIndex))
                    {
                        maxPart = localMax;
                        maxPartIndex = localIndex;
                    }
                }
            });
            sw.Stop();
            Console.WriteLine($"Partitioner (блоки по 100 000):    {sw.ElapsedMilliseconds,6} мс, Максимум: {maxPart} (позиция {maxPartIndex})");

            // PLINQ
            sw.Restart();
            int maxPlinq = data.AsParallel().Max();
            sw.Stop();
            Console.WriteLine($"PLINQ Max:                         {sw.ElapsedMilliseconds,6} мс, Максимум: {maxPlinq}");
            Console.WriteLine($"Максимумы совпадают: {maxSeq == maxLock && maxSeq == maxLocal && maxSeq == maxPart && maxSeq == maxPlinq && maxSeqIndex == maxPartIndex}, " +
                              $"ускорение (локальные): {(double)seqTime / Math.Max(localTime, 1):F1} раз");
            Console.WriteLine("Сложение и сравнение дешевле чтения памяти, поэтому выигрыш упирается в пропускную " +
                              "способность памяти; синхронизация на каждой итерации (Interlocked/lock) делает хуже последовательного.");

            Console.WriteLine("\n=== Вариант 1 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 2.
// ПАРАЛЛЕЛЬНАЯ ФИЛЬТРАЦИЯ КОЛЛЕКЦИЙ: PLINQ, ПОЛЬЗОВАТЕЛЬСКИЕ УСЛОВИЯ
// Используются только средства из методички: последовательный LINQ,
// AsParallel(), WithDegreeOfParallelism(), AsOrdered(), Parallel.ForEach
// с локальными для потока списками и lock при агрегации, Stopwatch.
// =====================================================================
namespace Variant2
{
    class Program
    {
        // Пользовательское условие 1: число простое (дорогая проверка делением)
        static bool IsPrime(int n)
        {
            if (n < 2) return false;
            for (int d = 2; (long)d * d <= n; d++)
            {
                if (n % d == 0) return false;
            }
            return true;
        }

        // Пользовательское условие 2: сумма цифр числа кратна divisor
        static bool DigitSumDivisibleBy(int n, int divisor)
        {
            int sum = 0;
            while (n > 0)
            {
                sum += n % 10;
                n /= 10;
            }
            return sum % divisor == 0;
        }

        // Сравнение LINQ / PLINQ / PLINQ AsOrdered / Parallel.ForEach для одного условия
        static void CompareFilter(List<int> data, string name, Func<int, bool> predicate)
        {
            Console.WriteLine($"--- Условие: {name} ---");

            // --- Последовательный LINQ ---
            Stopwatch sw = Stopwatch.StartNew();
            List<int> seqResult = data.Where(predicate).ToList();
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательный LINQ:       {seqTime,6} мс, найдено: {seqResult.Count}");

            // --- PLINQ (по умолчанию) ---
            sw.Restart();
            List<int> plinqResult = data.AsParallel().Where(predicate).ToList();
            sw.Stop();
            long plinqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"PLINQ (по умолчанию):        {plinqTime,6} мс, найдено: {plinqResult.Count}");

            // --- PLINQ с сохранением порядка ---
            sw.Restart();
            List<int> orderedResult = data.AsParallel().AsOrdered().Where(predicate).ToList();
            sw.Stop();
            Console.WriteLine($"PLINQ (упорядоченный):       {sw.ElapsedMilliseconds,6} мс, первые: {string.Join(", ", orderedResult.Take(3))}");

            // --- Parallel.ForEach с локальными для потока списками ---
            sw.Restart();
            List<int> forEachResult = new List<int>();
            object listLock = new object();
            Parallel.ForEach(data,
                () => new List<int>(),
                (val, state, local) =>
                {
                    if (predicate(val)) local.Add(val);
                    return local;
                },
                local => { lock (listLock) forEachResult.AddRange(local); }
            );
            sw.Stop();
            Console.WriteLine($"Parallel.ForEach (локальные):{sw.ElapsedMilliseconds,6} мс, найдено: {forEachResult.Count}");

            // Проверка: AsOrdered дает тот же порядок, что и LINQ; остальные – то же количество
            bool same = seqResult.SequenceEqual(orderedResult)
                        && plinqResult.Count == seqResult.Count
                        && forEachResult.Count == seqResult.Count;
            Console.WriteLine($"Результаты совпадают: {same}, ускорение PLINQ: {(double)seqTime / Math.Max(plinqTime, 1):F1} раз\n");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 2: ПАРАЛЛЕЛЬНАЯ ФИЛЬТРАЦИЯ КОЛЛЕКЦИЙ ===\n");

            // Генерация данных
            const int N = 5_000_000;
            Random rand = new Random();
            List<int> data = Enumerable.Range(0, N)
                .Select(i => rand.Next(1, 100_000_000))
                .ToList();
            Console.WriteLine($"Сгенерировано {N:N0} чисел в диапазоне [1; 100 000 000)\n");

            // 1. Набор пользовательских условий: от дешевого к дорогому
            CompareFilter(data, "четные числа (дешевое условие)", x => x % 2 == 0);
            CompareFilter(data, "простые числа (дорогое условие)", x => IsPrime(x));
            CompareFilter(data, "простые и сумма цифр кратна 5 (составное условие)",
                x => IsPrime(x) && DigitSumDivisibleBy(x, 5));

            // 2. Зависимость времени от степени параллелизма (дорогое условие)
            Console.WriteLine("--- Простые числа: WithDegreeOfParallelism ---");
            for (int degree = 1; degree <= Environment.ProcessorCount; degree *= 2)
            {
                Stopwatch sw = Stopwatch.StartNew();
                int count = data.AsParallel().WithDegreeOfParallelism(degree).Where(x => IsPrime(x)).Count();
                sw.Stop();
                Console.WriteLine($"Степень = {degree,2}: {sw.ElapsedMilliseconds,6} мс, найдено: {count}");
            }

            // 3. Интерактивная фильтрация по условию пользователя:
            //    x > min и x кратно divisor. Ввод 'exit' – возврат в меню.
            Console.WriteLine("\n=== Пользовательское условие: x > min и x % divisor == 0 ===");
            Console.WriteLine("Введите два числа через пробел: min divisor (или 'exit'):");
            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (input == null || input.ToLower() == "exit") break;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int min)
                    && int.TryParse(parts[1], out int divisor)
                    && divisor != 0)
                {
                    CompareFilter(data, $"x > {min} и x % {divisor} == 0", x => x > min && x % divisor == 0);
                }
                else
                {
                    Console.WriteLine("Неверный ввод. Пример: 50000000 7");
                }
            }

            Console.WriteLine("\n=== Вариант 2 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 3.
// МАТРИЧНЫЕ ВЫЧИСЛЕНИЯ: УМНОЖЕНИЕ, ТРАНСПОНИРОВАНИЕ
// Строки результата независимы, поэтому распараллеливание идет по строкам
// (Parallel.For), по блокам строк (Partitioner) и через PLINQ.
// =====================================================================
namespace Variant3
{
    class Program
    {
        // Вычисление одной строки i произведения C = A * B
        static void MultiplyRow(double[,] a, double[,] b, double[,] c, int i, int n)
        {
            for (int j = 0; j < n; j++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++) sum += a[i, k] * b[k, j];
                c[i, j] = sum;
            }
        }

        // Строка i произведения через транспонированную B: bt[j, k] = b[k, j],
        // обход bt идет по строке – меньше кэш-промахов
        static void MultiplyRowTransposed(double[,] a, double[,] bt, double[,] c, int i, int n)
        {
            for (int j = 0; j < n; j++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++) sum += a[i, k] * bt[j, k];
                c[i, j] = sum;
            }
        }

        static double[,] RandomMatrix(int rows, int cols, Random rand)
        {
            double[,] m = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    m[i, j] = rand.NextDouble() * 10;
            return m;
        }

        static bool Same(double[,] x, double[,] y)
        {
            for (int i = 0; i < x.GetLength(0); i++)
                for (int j = 0; j < x.GetLength(1); j++)
                    if (Math.Abs(x[i, j] - y[i, j]) > 1e-9) return false;
            return true;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 3: МАТРИЧНЫЕ ВЫЧИСЛЕНИЯ ===\n");
            Random rand = new Random();

            // 1. Умножение матриц
            const int n = 600;
            double[,] a = RandomMatrix(n, n, rand);
            double[,] b = RandomMatrix(n, n, rand);
            Console.WriteLine($"--- Умножение матриц {n}x{n} ---");

            // Последовательно
            double[,] cSeq = new double[n, n];
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++) MultiplyRow(a, b, cSeq, i, n);
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:                 {seqTime,6} мс");

            // Parallel.For по строкам
            double[,] cPar = new double[n, n];
            sw.Restart();
            Parallel.For(0, n, i => MultiplyRow(a, b, cPar, i, n));
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (по строкам):       {parTime,6} мс");

            // Partitioner: блоки по 20 строк
            double[,] cPart = new double[n, n];
            sw.Restart();
            Parallel.ForEach(Partitioner.Create(0, n, 20), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++) MultiplyRow(a, b, cPart, i, n);
            });
            sw.Stop();
            Console.WriteLine($"Partitioner (блоки по 20 строк): {sw.ElapsedMilliseconds,6} мс");

            // PLINQ: каждая строка – отдельный элемент запроса, порядок сохраняется
            double[,] cPlinq = new double[n, n];
            sw.Restart();
            double[][] rows = Enumerable.Range(0, n)
                .AsParallel()
                .AsOrdered()
                .Select(i =>
                {
                    double[] row = new double[n];
                    for (int j = 0; j < n; j++)
                    {
                        double sum = 0;
                        for (int k = 0; k < n; k++) sum += a[i, k] * b[k, j];
                        row[j] = sum;
                    }
                    return row;
                })
                .ToArray();
            sw.Stop();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    cPlinq[i, j] = rows[i][j];
            Console.WriteLine($"PLINQ (AsOrdered):               {sw.ElapsedMilliseconds,6} мс");

            // Parallel.For + транспонированная B
            double[,] bt = new double[n, n];
            Parallel.For(0, n, i => { for (int j = 0; j < n; j++) bt[j, i] = b[i, j]; });
            double[,] cTr = new double[n, n];
            sw.Restart();
            Parallel.For(0, n, i => MultiplyRowTransposed(a, bt, cTr, i, n));
            sw.Stop();
            Console.WriteLine($"Parallel.For + транспонир. B:    {sw.ElapsedMilliseconds,6} мс");

            Console.WriteLine($"Результаты совпадают: {Same(cSeq, cPar) && Same(cSeq, cPart) && Same(cSeq, cPlinq) && Same(cSeq, cTr)}, " +
                              $"ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз");
            Console.WriteLine($"C[0,0] = {cSeq[0, 0]:F4}, C[{n - 1},{n - 1}] = {cSeq[n - 1, n - 1]:F4}\n");

            // 2. Транспонирование
            const int rowsCount = 3000, colsCount = 3000;
            double[,] m = RandomMatrix(rowsCount, colsCount, rand);
            Console.WriteLine($"--- Транспонирование матрицы {rowsCount}x{colsCount} ---");

            double[,] tSeq = new double[colsCount, rowsCount];
            sw.Restart();
            for (int i = 0; i < rowsCount; i++)
                for (int j = 0; j < colsCount; j++)
                    tSeq[j, i] = m[i, j];
            sw.Stop();
            seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:                 {seqTime,6} мс");

            double[,] tPar = new double[colsCount, rowsCount];
            sw.Restart();
            Parallel.For(0, rowsCount, i =>
            {
                for (int j = 0; j < colsCount; j++) tPar[j, i] = m[i, j];
            });
            sw.Stop();
            parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (по строкам):       {parTime,6} мс");

            double[,] tPart = new double[colsCount, rowsCount];
            sw.Restart();
            Parallel.ForEach(Partitioner.Create(0, rowsCount, 100), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    for (int j = 0; j < colsCount; j++)
                        tPart[j, i] = m[i, j];
            });
            sw.Stop();
            Console.WriteLine($"Partitioner (блоки по 100 строк):{sw.ElapsedMilliseconds,6} мс");

            Console.WriteLine($"Результаты совпадают: {Same(tSeq, tPar) && Same(tSeq, tPart)}, " +
                              $"ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз");
            Console.WriteLine("Транспонирование почти не считает, а только копирует память, " +
                              "поэтому выигрыш от потоков меньше, чем у умножения.");

            Console.WriteLine("\n=== Вариант 3 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 4.
// ОБРАБОТКА ИЗОБРАЖЕНИЙ: ФИЛЬТРЫ, ПРЕОБРАЗОВАНИЯ
// Изображение 2000x2000 в оттенках серого (0..255) как double[,], как в
// задании 5. Каждый фильтр запускается последовательно, Parallel.For по
// строкам, Partitioner по блокам строк и PLINQ; результаты сверяются.
// =====================================================================
namespace Variant4
{
    class Program
    {
        // --- Поточечные преобразования ---
        static double Invert(double[,] img, int y, int x) => 255 - img[y, x];

        static double Threshold(double[,] img, int y, int x) => img[y, x] > 128 ? 255 : 0;

        // Яркость +20 и контраст x1.5 с ограничением в [0; 255]
        static double BrightnessContrast(double[,] img, int y, int x)
            => Math.Clamp((img[y, x] - 128) * 1.5 + 128 + 20, 0, 255);

        // --- Фильтры по окрестности ---
        // Размытие: среднее по окну 3x3 (на краях – по существующим соседям)
        static double BoxBlur(double[,] img, int y, int x)
        {
            int h = img.GetLength(0), w = img.GetLength(1);
            double sum = 0;
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int yy = y + dy, xx = x + dx;
                    if (yy >= 0 && yy < h && xx >= 0 && xx < w) { sum += img[yy, xx]; count++; }
                }
            }
            return sum / count;
        }

        // Выделение границ: оператор Собеля
        static double Sobel(double[,] img, int y, int x)
        {
            int h = img.GetLength(0), w = img.GetLength(1);
            if (y == 0 || x == 0 || y == h - 1 || x == w - 1) return 0;
            double gx = -img[y - 1, x - 1] - 2 * img[y, x - 1] - img[y + 1, x - 1]
                        + img[y - 1, x + 1] + 2 * img[y, x + 1] + img[y + 1, x + 1];
            double gy = -img[y - 1, x - 1] - 2 * img[y - 1, x] - img[y - 1, x + 1]
                        + img[y + 1, x - 1] + 2 * img[y + 1, x] + img[y + 1, x + 1];
            return Math.Min(255, Math.Sqrt(gx * gx + gy * gy));
        }

        static bool Same(double[,] x, double[,] y)
        {
            for (int i = 0; i < x.GetLength(0); i++)
                for (int j = 0; j < x.GetLength(1); j++)
                    if (x[i, j] != y[i, j]) return false;
            return true;
        }

        // Запуск одного фильтра четырьмя способами
        static double[,] RunFilter(string name, double[,] image, Func<double[,], int, int, double> filter)
        {
            int height = image.GetLength(0), width = image.GetLength(1);
            Console.WriteLine($"--- {name} ---");

            // Последовательно
            double[,] seq = new double[height, width];
            Stopwatch sw = Stopwatch.StartNew();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    seq[y, x] = filter(image, y, x);
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;

            // Parallel.For по строкам
            double[,] par = new double[height, width];
            sw.Restart();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++) par[y, x] = filter(image, y, x);
            });
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;

            // Partitioner: блоки по 100 строк
            double[,] part = new double[height, width];
            sw.Restart();
            Parallel.ForEach(Partitioner.Create(0, height, 100), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                    for (int x = 0; x < width; x++)
                        part[y, x] = filter(image, y, x);
            });
            sw.Stop();
            long partTime = sw.ElapsedMilliseconds;

            // PLINQ по пикселям, AsOrdered – чтобы пиксели легли на свои места
            sw.Restart();
            double[] flat = Enumerable.Range(0, height * width)
                .AsParallel()
                .AsOrdered()
                .Select(i => filter(image, i / width, i % width))
                .ToArray();
            sw.Stop();
            long plinqTime = sw.ElapsedMilliseconds;
            double[,] plinq = new double[height, width];
            for (int i = 0; i < flat.Length; i++) plinq[i / width, i % width] = flat[i];

            bool same = Same(seq, par) && Same(seq, part) && Same(seq, plinq);
            Console.WriteLine($"Последовательно: {seqTime,5} мс | Parallel.For: {parTime,4} мс | " +
                              $"Partitioner: {partTime,4} мс | PLINQ: {plinqTime,4} мс");
            Console.WriteLine($"Результаты совпадают: {same}, ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз\n");
            return par;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 4: ОБРАБОТКА ИЗОБРАЖЕНИЙ ===\n");

            const int width = 2000;
            const int height = 2000;
            double[,] image = new double[height, width];
            Random rand = new Random();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    image[y, x] = rand.Next(0, 256);
            Console.WriteLine($"Изображение {width}x{height}, оттенки серого 0..255\n");

            // Преобразования (дешевые, поточечные)
            RunFilter("Преобразование: негатив", image, Invert);
            RunFilter("Преобразование: бинаризация (порог 128)", image, Threshold);
            RunFilter("Преобразование: яркость +20, контраст x1.5", image, BrightnessContrast);

            // Фильтры (дороже, используют соседние пиксели)
            double[,] blurred = RunFilter("Фильтр: размытие 3x3", image, BoxBlur);
            double[,] edges = RunFilter("Фильтр: границы (Собель) по размытому изображению", blurred, Sobel);

            // Статистика результата через PLINQ
            double avg = Enumerable.Range(0, height * width).AsParallel().Average(i => edges[i / width, i % width]);
            Console.WriteLine($"Средняя яркость карты границ (PLINQ Average): {avg:F2}");
            Console.WriteLine("Поточечные преобразования дешевые – потоки дают мало; " +
                              "фильтры по окрестности дороже – выигрыш больше.");

            Console.WriteLine("\n=== Вариант 4 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 5.
// АНАЛИЗ ТЕКСТОВЫХ ДАННЫХ: ПАРАЛЛЕЛЬНЫЙ ПОИСК, ПОДСЧЕТ
// Текст – массив строк из случайных слов. Поиск слова (PLINQ, Parallel.For
// с локальным счетчиком, Parallel.For + state.Break() для первого вхождения),
// подсчет слов и букв, частотный словарь (локальные словари потоков и
// PLINQ GroupBy – хеш-разделение по ключу).
// =====================================================================
namespace Variant5
{
    class Program
    {
        static string RandomWord(Random rand)
        {
            int length = rand.Next(3, 9);
            char[] chars = new char[length];
            for (int i = 0; i < length; i++) chars[i] = (char)('a' + rand.Next(26));
            return new string(chars);
        }

        // Строка содержит слово целиком (а не как часть другого слова)
        static bool ContainsWord(string line, string word) => Array.IndexOf(line.Split(' '), word) >= 0;

        static int CountWord(string line, string word) => line.Split(' ').Count(w => w == word);

        // Поиск слова тремя способами + первое вхождение
        static void SearchWord(string[] lines, string word)
        {
            Console.WriteLine($"--- Поиск слова \"{word}\" ---");

            // Последовательный LINQ
            Stopwatch sw = Stopwatch.StartNew();
            int seqLines = lines.Count(l => ContainsWord(l, word));
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательный LINQ:       {seqTime,5} мс, строк со словом: {seqLines}");

            // PLINQ
            sw.Restart();
            int plinqLines = lines.AsParallel().Count(l => ContainsWord(l, word));
            sw.Stop();
            long plinqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"PLINQ:                       {plinqTime,5} мс, строк со словом: {plinqLines}");

            // Parallel.For с локальным счетчиком вхождений
            sw.Restart();
            int occurrences = 0;
            Parallel.For(0, lines.Length,
                () => 0,
                (i, state, local) => local + CountWord(lines[i], word),
                local => Interlocked.Add(ref occurrences, local)
            );
            sw.Stop();
            Console.WriteLine($"Parallel.For (локальные):    {sw.ElapsedMilliseconds,5} мс, всего вхождений: {occurrences}");

            // Первое вхождение: state.Break() гарантирует, что все строки
            // с меньшими индексами будут проверены, поэтому минимум точный
            sw.Restart();
            int firstLine = -1;
            object lockObj = new object();
            Parallel.For(0, lines.Length, (i, state) =>
            {
                if (ContainsWord(lines[i], word))
                {
                    lock (lockObj) { if (firstLine == -1 || i < firstLine) firstLine = i; }
                    state.Break();
                }
            });
            sw.Stop();
            int firstSeq = Array.FindIndex(lines, l => ContainsWord(l, word));
            Console.WriteLine($"Первое вхождение (Break):    {sw.ElapsedMilliseconds,5} мс, строка: {firstLine} (последовательно: {firstSeq})");
            Console.WriteLine($"Результаты совпадают: {seqLines == plinqLines && firstLine == firstSeq}, " +
                              $"ускорение PLINQ: {(double)seqTime / Math.Max(plinqTime, 1):F1} раз\n");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 5: АНАЛИЗ ТЕКСТОВЫХ ДАННЫХ ===\n");

            // Генерация текста: словарь из 5000 слов, 400 000 строк по 12 слов.
            // Pow(…, 3) делает часть слов частыми, а часть – редкими, как в живом тексте.
            Random rand = new Random();
            string[] vocabulary = Enumerable.Range(0, 5000).Select(i => RandomWord(rand)).Distinct().ToArray();
            const int L = 400_000;
            const int WordsInLine = 12;
            string[] lines = new string[L];
            for (int i = 0; i < L; i++)
            {
                string[] words = new string[WordsInLine];
                for (int j = 0; j < WordsInLine; j++)
                    words[j] = vocabulary[(int)(Math.Pow(rand.NextDouble(), 3) * vocabulary.Length)];
                lines[i] = string.Join(" ", words);
            }
            Console.WriteLine($"Текст: {L:N0} строк, {L * WordsInLine:N0} слов, словарь {vocabulary.Length} слов");
            Console.WriteLine($"Пример строки: {lines[0]}\n");

            object lockObj = new object();

            // 1. Подсчет слов и символов
            Console.WriteLine("--- Подсчет слов и символов ---");
            Stopwatch sw = Stopwatch.StartNew();
            long wordsSeq = lines.Sum(l => (long)l.Split(' ').Length);
            long charsSeq = lines.Sum(l => (long)l.Length);
            sw.Stop();
            Console.WriteLine($"Последовательный LINQ:       {sw.ElapsedMilliseconds,5} мс, слов: {wordsSeq}, символов: {charsSeq}");

            sw.Restart();
            long wordsPlinq = lines.AsParallel().Sum(l => (long)l.Split(' ').Length);
            long charsPlinq = lines.AsParallel().Sum(l => (long)l.Length);
            sw.Stop();
            Console.WriteLine($"PLINQ:                       {sw.ElapsedMilliseconds,5} мс, слов: {wordsPlinq}, символов: {charsPlinq}\n");

            // 2. Частота букв: локальный массив int[26] на поток + слияние под lock
            Console.WriteLine("--- Частота букв ---");
            sw.Restart();
            int[] lettersSeq = new int[26];
            foreach (string line in lines)
                foreach (char c in line)
                    if (c != ' ') lettersSeq[c - 'a']++;
            sw.Stop();
            Console.WriteLine($"Последовательно:             {sw.ElapsedMilliseconds,5} мс");

            sw.Restart();
            int[] lettersPar = new int[26];
            Parallel.For(0, lines.Length,
                () => new int[26],
                (i, state, local) =>
                {
                    foreach (char c in lines[i])
                        if (c != ' ') local[c - 'a']++;
                    return local;
                },
                local => { lock (lockObj) { for (int k = 0; k < 26; k++) lettersPar[k] += local[k]; } }
            );
            sw.Stop();
            Console.WriteLine($"Parallel.For (локальные):    {sw.ElapsedMilliseconds,5} мс");
            Console.WriteLine($"Совпадают: {lettersSeq.SequenceEqual(lettersPar)}, " +
                              $"a={lettersPar[0]}, e={lettersPar['e' - 'a']}, z={lettersPar[25]}\n");

            // 3. Частотный словарь слов
            Console.WriteLine("--- Частотный словарь слов ---");
            sw.Restart();
            Dictionary<string, int> freqSeq = new Dictionary<string, int>();
            foreach (string line in lines)
                foreach (string w in line.Split(' '))
                    freqSeq[w] = freqSeq.TryGetValue(w, out int cnt) ? cnt + 1 : 1;
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:             {seqTime,5} мс, разных слов: {freqSeq.Count}");

            // Parallel.ForEach: у каждого потока свой словарь, слияние один раз в конце
            sw.Restart();
            Dictionary<string, int> freqPar = new Dictionary<string, int>();
            Parallel.ForEach(lines,
                () => new Dictionary<string, int>(),
                (line, state, local) =>
                {
                    foreach (string w in line.Split(' '))
                        local[w] = local.TryGetValue(w, out int cnt) ? cnt + 1 : 1;
                    return local;
                },
                local =>
                {
                    lock (lockObj)
                    {
                        foreach (var pair in local)
                            freqPar[pair.Key] = freqPar.TryGetValue(pair.Key, out int cnt) ? cnt + pair.Value : pair.Value;
                    }
                }
            );
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.ForEach (локальные):{parTime,5} мс, разных слов: {freqPar.Count}");

            // PLINQ GroupBy: элементы распределяются по хешу ключа
            sw.Restart();
            Dictionary<string, int> freqPlinq = lines
                .AsParallel()
                .SelectMany(l => l.Split(' '))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());
            sw.Stop();
            Console.WriteLine($"PLINQ GroupBy:               {sw.ElapsedMilliseconds,5} мс, разных слов: {freqPlinq.Count}");

            bool same = freqSeq.Count == freqPar.Count && freqSeq.Count == freqPlinq.Count
                        && freqSeq.All(p => freqPar[p.Key] == p.Value && freqPlinq[p.Key] == p.Value);
            Console.WriteLine($"Словари совпадают: {same}, ускорение Parallel.ForEach: {(double)seqTime / Math.Max(parTime, 1):F1} раз");
            var top = freqPar.OrderByDescending(p => p.Value).Take(5);
            Console.WriteLine($"Топ-5 слов: {string.Join(", ", top.Select(p => $"{p.Key} ({p.Value})"))}\n");

            // 4. Поиск: частое слово и редкое слово
            SearchWord(lines, top.First().Key);
            SearchWord(lines, freqPar.OrderBy(p => p.Value).First().Key);

            // 5. Интерактивный поиск. Ввод 'exit' – возврат в меню.
            Console.WriteLine("=== Интерактивный поиск слова ===");
            Console.WriteLine("Введите слово (или 'exit'):");
            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (input == null || input.ToLower() == "exit") break;
                if (!string.IsNullOrWhiteSpace(input)) SearchWord(lines, input.Trim().ToLower());
            }

            Console.WriteLine("\n=== Вариант 5 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 6.
// ЧИСЛЕННЫЕ МЕТОДЫ: ИНТЕГРИРОВАНИЕ, ОПТИМИЗАЦИЯ
// Интеграл – метод средних прямоугольников (сумма независимых слагаемых →
// локальная агрегация). Оптимизация – поиск минимума функции двух
// переменных перебором по сетке (локальный минимум потока + lock).
// =====================================================================
namespace Variant6
{
    class Program
    {
        // Метод средних прямоугольников: I = h * сумма f(a + (i + 0.5) * h)
        static double IntegrateSeq(Func<double, double> f, double a, double b, int n)
        {
            double h = (b - a) / n;
            double sum = 0;
            for (int i = 0; i < n; i++) sum += f(a + (i + 0.5) * h);
            return sum * h;
        }

        static double IntegrateParallel(Func<double, double> f, double a, double b, int n)
        {
            double h = (b - a) / n;
            double sum = 0;
            object lockObj = new object();
            Parallel.For(0, n,
                () => 0.0,
                (i, state, local) => local + f(a + (i + 0.5) * h),
                local => { lock (lockObj) sum += local; }
            );
            return sum * h;
        }

        static double IntegratePartitioner(Func<double, double> f, double a, double b, int n)
        {
            double h = (b - a) / n;
            double sum = 0;
            object lockObj = new object();
            Parallel.ForEach(Partitioner.Create(0, n, 100000), range =>
            {
                double localSum = 0;
                for (int i = range.Item1; i < range.Item2; i++) localSum += f(a + (i + 0.5) * h);
                lock (lockObj) sum += localSum;
            });
            return sum * h;
        }

        static double IntegratePlinq(Func<double, double> f, double a, double b, int n)
        {
            double h = (b - a) / n;
            return Enumerable.Range(0, n).AsParallel().Sum(i => f(a + (i + 0.5) * h)) * h;
        }

        static void RunIntegral(string name, Func<double, double> f, double a, double b, int n, double exact)
        {
            Console.WriteLine($"--- {name}, точное значение {exact:F10} ---");

            Stopwatch sw = Stopwatch.StartNew();
            double seq = IntegrateSeq(f, a, b, n);
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:            {seqTime,5} мс, I = {seq:F10}, погрешность {Math.Abs(seq - exact):E2}");

            sw.Restart();
            double par = IntegrateParallel(f, a, b, n);
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (локальные):   {parTime,5} мс, I = {par:F10}, погрешность {Math.Abs(par - exact):E2}");

            sw.Restart();
            double part = IntegratePartitioner(f, a, b, n);
            sw.Stop();
            Console.WriteLine($"Partitioner (блоки 100 000):{sw.ElapsedMilliseconds,5} мс, I = {part:F10}, погрешность {Math.Abs(part - exact):E2}");

            sw.Restart();
            double plinq = IntegratePlinq(f, a, b, n);
            sw.Stop();
            Console.WriteLine($"PLINQ Sum:                  {sw.ElapsedMilliseconds,5} мс, I = {plinq:F10}, погрешность {Math.Abs(plinq - exact):E2}");
            Console.WriteLine($"Ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз " +
                              "(последние знаки могут отличаться: сумма double зависит от порядка сложения)\n");
        }

        // Функция Растригина со смещенным минимумом: f(1.5; -0.5) = 0, много локальных минимумов
        static double Rastrigin(double x, double y)
        {
            double dx = x - 1.5, dy = y + 0.5;
            return 20 + dx * dx - 10 * Math.Cos(2 * Math.PI * dx) + dy * dy - 10 * Math.Cos(2 * Math.PI * dy);
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 6: ЧИСЛЕННЫЕ МЕТОДЫ ===\n");

            // 1. Интегрирование
            const int n = 50_000_000;
            Console.WriteLine($"Метод средних прямоугольников, разбиений: {n:N0}\n");
            RunIntegral("Интеграл 4/(1+x^2) на [0; 1] (= pi)", x => 4 / (1 + x * x), 0, 1, n, Math.PI);
            RunIntegral("Интеграл sin(x)*exp(-x) на [0; pi]", x => Math.Sin(x) * Math.Exp(-x), 0, Math.PI, n,
                (1 + Math.Exp(-Math.PI)) / 2);

            // 2. Оптимизация: минимум функции перебором по сетке
            const int grid = 4000;
            const double min = -5, max = 5;
            double step = (max - min) / (grid - 1);
            Console.WriteLine($"--- Оптимизация: минимум функции Растригина на [-5; 5]^2, сетка {grid}x{grid} ---");
            Console.WriteLine("Точный минимум: f(1.5; -0.5) = 0");

            // Последовательно
            Stopwatch sw = Stopwatch.StartNew();
            (double Value, double X, double Y) bestSeq = (double.MaxValue, 0, 0);
            for (int i = 0; i < grid; i++)
            {
                double x = min + i * step;
                for (int j = 0; j < grid; j++)
                {
                    double y = min + j * step;
                    double v = Rastrigin(x, y);
                    if (v < bestSeq.Value) bestSeq = (v, x, y);
                }
            }
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:            {seqTime,5} мс, f({bestSeq.X:F4}; {bestSeq.Y:F4}) = {bestSeq.Value:F6}");

            // Parallel.For по строкам сетки: у потока свой лучший результат, сравнение под lock в конце
            sw.Restart();
            (double Value, double X, double Y) bestPar = (double.MaxValue, 0, 0);
            object lockObj = new object();
            Parallel.For(0, grid,
                () => (Value: double.MaxValue, X: 0.0, Y: 0.0),
                (i, state, local) =>
                {
                    double x = min + i * step;
                    for (int j = 0; j < grid; j++)
                    {
                        double y = min + j * step;
                        double v = Rastrigin(x, y);
                        if (v < local.Value) local = (v, x, y);
                    }
                    return local;
                },
                local => { lock (lockObj) { if (local.Value < bestPar.Value) bestPar = local; } }
            );
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (локальные):   {parTime,5} мс, f({bestPar.X:F4}; {bestPar.Y:F4}) = {bestPar.Value:F6}");

            // PLINQ: минимальное значение по всем узлам сетки
            sw.Restart();
            double bestPlinq = Enumerable.Range(0, grid * grid)
                .AsParallel()
                .Min(k => Rastrigin(min + (k / grid) * step, min + (k % grid) * step));
            sw.Stop();
            Console.WriteLine($"PLINQ Min:                  {sw.ElapsedMilliseconds,5} мс, минимум = {bestPlinq:F6}");
            Console.WriteLine($"Минимумы совпадают: {bestSeq.Value == bestPar.Value && bestSeq.Value == bestPlinq}, " +
                              $"ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз");

            Console.WriteLine("\n=== Вариант 6 Выполнен ===\n");
        }
    }
}

// =====================================================================
// ИНДИВИДУАЛЬНОЕ ЗАДАНИЕ. ВАРИАНТ 7.
// СОРТИРОВКА И ПОИСК: ПАРАЛЛЕЛЬНЫЕ АЛГОРИТМЫ
// Сортировка: Array.Sort, LINQ OrderBy, PLINQ OrderBy и параллельная
// блочная сортировка слиянием (блоки сортируются в Parallel.ForEach по
// Partitioner, затем попарно сливаются в Parallel.For).
// Поиск: линейный с state.Break(), подсчет по условию в PLINQ, пакет
// бинарных поисков в Parallel.For с локальным счетчиком.
// =====================================================================
namespace Variant7
{
    class Program
    {
        // Слияние двух отсортированных участков src[lo..mid) и src[mid..hi) в dst[lo..hi)
        static void Merge(int[] src, int[] dst, int lo, int mid, int hi)
        {
            int i = lo, j = mid, k = lo;
            while (i < mid && j < hi) dst[k++] = src[i] <= src[j] ? src[i++] : src[j++];
            while (i < mid) dst[k++] = src[i++];
            while (j < hi) dst[k++] = src[j++];
        }

        static int[] ParallelMergeSort(int[] input)
        {
            int n = input.Length;
            int[] src = (int[])input.Clone();
            int[] dst = new int[n];

            // 1. Делим массив на блоки по числу процессоров и сортируем блоки параллельно
            int parts = Environment.ProcessorCount;
            int chunk = (n + parts - 1) / parts;
            Parallel.ForEach(Partitioner.Create(0, n, chunk), range =>
                Array.Sort(src, range.Item1, range.Item2 - range.Item1));

            List<(int Lo, int Hi)> runs = new List<(int Lo, int Hi)>();
            for (int lo = 0; lo < n; lo += chunk) runs.Add((lo, Math.Min(lo + chunk, n)));

            // 2. Попарно сливаем соседние блоки, пока не останется один; пары сливаются параллельно
            while (runs.Count > 1)
            {
                List<(int Lo, int Hi)> current = runs;
                int[] from = src, to = dst;
                int pairs = (current.Count + 1) / 2;
                Parallel.For(0, pairs, p =>
                {
                    int r = p * 2;
                    if (r + 1 < current.Count)
                        Merge(from, to, current[r].Lo, current[r].Hi, current[r + 1].Hi);
                    else
                        Array.Copy(from, current[r].Lo, to, current[r].Lo, current[r].Hi - current[r].Lo);
                });

                runs = new List<(int Lo, int Hi)>();
                for (int r = 0; r < current.Count; r += 2)
                    runs.Add(r + 1 < current.Count ? (current[r].Lo, current[r + 1].Hi) : current[r]);
                (src, dst) = (dst, src);
            }
            return src;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ВАРИАНТ 7: СОРТИРОВКА И ПОИСК ===\n");

            const int N = 5_000_000;
            int[] data = new int[N];
            Random rand = new Random();
            for (int i = 0; i < N; i++) data[i] = rand.Next(0, 100_000_000);
            Console.WriteLine($"Массив: {N:N0} чисел в диапазоне [0; 100 000 000)\n");

            // 1. Сортировка
            Console.WriteLine("--- Сортировка ---");
            int[] sortedSeq = (int[])data.Clone();
            Stopwatch sw = Stopwatch.StartNew();
            Array.Sort(sortedSeq);
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Array.Sort (последовательно):     {seqTime,5} мс");

            sw.Restart();
            int[] sortedLinq = data.OrderBy(x => x).ToArray();
            sw.Stop();
            long linqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"LINQ OrderBy (последовательно):   {linqTime,5} мс");

            sw.Restart();
            int[] sortedPlinq = data.AsParallel().OrderBy(x => x).ToArray();
            sw.Stop();
            long plinqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"PLINQ OrderBy:                    {plinqTime,5} мс");

            sw.Restart();
            int[] sortedMerge = ParallelMergeSort(data);
            sw.Stop();
            long mergeTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Параллельная сортировка слиянием: {mergeTime,5} мс ({Environment.ProcessorCount} блоков)");

            bool sameSort = sortedSeq.SequenceEqual(sortedLinq) && sortedSeq.SequenceEqual(sortedPlinq)
                            && sortedSeq.SequenceEqual(sortedMerge);
            Console.WriteLine($"Результаты совпадают: {sameSort}");
            Console.WriteLine($"Ускорение: PLINQ к LINQ – {(double)linqTime / Math.Max(plinqTime, 1):F1} раз, " +
                              $"слияние к Array.Sort – {(double)seqTime / Math.Max(mergeTime, 1):F1} раз\n");

            // 2. Линейный поиск первого вхождения в неотсортированном массиве
            Console.WriteLine("--- Линейный поиск первого вхождения ---");
            int target = data[N * 3 / 4];
            sw.Restart();
            int firstSeq = -1;
            for (int i = 0; i < N; i++) { if (data[i] == target) { firstSeq = i; break; } }
            sw.Stop();
            Console.WriteLine($"Последовательно:                  {sw.ElapsedMilliseconds,5} мс, {target} на позиции {firstSeq}");

            sw.Restart();
            int firstPar = -1;
            object lockObj = new object();
            Parallel.For(0, N, (i, state) =>
            {
                if (data[i] == target)
                {
                    lock (lockObj) { if (firstPar == -1 || i < firstPar) firstPar = i; }
                    state.Break();
                }
            });
            sw.Stop();
            Console.WriteLine($"Parallel.For + state.Break():     {sw.ElapsedMilliseconds,5} мс, {target} на позиции {firstPar}");
            Console.WriteLine($"Позиции совпадают: {firstSeq == firstPar}\n");

            // 3. Подсчет элементов по условию (диапазон значений)
            Console.WriteLine("--- Подсчет элементов в диапазоне [40 000 000; 60 000 000] ---");
            sw.Restart();
            int countSeq = data.Count(x => x >= 40_000_000 && x <= 60_000_000);
            sw.Stop();
            Console.WriteLine($"LINQ Count:                       {sw.ElapsedMilliseconds,5} мс, найдено: {countSeq}");

            sw.Restart();
            int countPlinq = data.AsParallel().Count(x => x >= 40_000_000 && x <= 60_000_000);
            sw.Stop();
            Console.WriteLine($"PLINQ Count:                      {sw.ElapsedMilliseconds,5} мс, найдено: {countPlinq}");

            // Тот же ответ в отсортированном массиве – двумя бинарными поисками
            int left = Array.BinarySearch(sortedSeq, 40_000_000);
            if (left < 0) left = ~left;
            while (left > 0 && sortedSeq[left - 1] == 40_000_000) left--;
            int right = Array.BinarySearch(sortedSeq, 60_000_001);
            if (right < 0) right = ~right;
            while (right > 0 && sortedSeq[right - 1] == 60_000_001) right--;
            Console.WriteLine($"Бинарный поиск по сортированному: найдено {right - left}, совпадает: {countSeq == countPlinq && countSeq == right - left}\n");

            // 4. Пакет бинарных поисков: каждый запрос независим
            const int Q = 2_000_000;
            int[] queries = new int[Q];
            for (int i = 0; i < Q; i++) queries[i] = rand.Next(0, 100_000_000);
            Console.WriteLine($"--- Пакет из {Q:N0} бинарных поисков в отсортированном массиве ---");

            sw.Restart();
            int foundSeq = 0;
            for (int i = 0; i < Q; i++) if (Array.BinarySearch(sortedSeq, queries[i]) >= 0) foundSeq++;
            sw.Stop();
            seqTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Последовательно:                  {seqTime,5} мс, найдено: {foundSeq}");

            sw.Restart();
            int foundPar = 0;
            Parallel.For(0, Q,
                () => 0,
                (i, state, local) => Array.BinarySearch(sortedSeq, queries[i]) >= 0 ? local + 1 : local,
                local => Interlocked.Add(ref foundPar, local)
            );
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Parallel.For (локальные):         {parTime,5} мс, найдено: {foundPar}");

            sw.Restart();
            int foundPlinq = queries.AsParallel().Count(q => Array.BinarySearch(sortedSeq, q) >= 0);
            sw.Stop();
            Console.WriteLine($"PLINQ Count:                      {sw.ElapsedMilliseconds,5} мс, найдено: {foundPlinq}");
            Console.WriteLine($"Результаты совпадают: {foundSeq == foundPar && foundSeq == foundPlinq}, " +
                              $"ускорение Parallel.For: {(double)seqTime / Math.Max(parTime, 1):F1} раз");

            Console.WriteLine("\n=== Вариант 7 Выполнен ===\n");
        }
    }
}
