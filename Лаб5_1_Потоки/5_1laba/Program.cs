// Лабораторная работа 5. Многопоточное и асинхронное программирование. Часть 1
//
// Все демонстрации собраны в одном файле Program.cs.
// Шаблоны кода из методички перенесены без изменений:
//   - Producer-Consumer с семафором (namespace ProducerConsumerLab)
//   - Искусственный Deadlock (namespace DeadlockLab)
//   - Именованный Mutex между процессами (namespace MutexCrossProcess)
// Плюс выполненные задания по методичке:
//   - Producer-Consumer: BufferSize 2/5/10, +1 producer/+1 consumer, DataItem (namespace ProducerConsumerTasks)
//   - Deadlock: исправление порядком захвата (namespace DeadlockFixed)
//   - Deadlock: обнаружение через Monitor.TryEnter(TimeSpan) (namespace DeadlockTimeout)
//
// Точка входа: _5_1laba.Program (задана через <StartupObject> в .csproj).

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// =====================================================================
// МЕНЮ ЗАПУСКА
// =====================================================================
namespace _5_1laba
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=== Лабораторная работа 5.1. Многопоточное и асинхронное программирование. Часть 1 ===");
                Console.WriteLine("1. Producer-Consumer с семафором (по методичке)");
                Console.WriteLine("2. Producer-Consumer: задания 2-4 (размер буфера / +1 producer,+1 consumer / объекты DataItem)");
                Console.WriteLine("3. Искусственный Deadlock (по методичке)");
                Console.WriteLine("4. Deadlock исправленный (одинаковый порядок захвата)");
                Console.WriteLine("5. Deadlock: обнаружение через Monitor.TryEnter(Timeout)");
                Console.WriteLine("6. Именованный Mutex между процессами (по методичке)");
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
                        ProducerConsumerLab.Program.ResetState();
                        ProducerConsumerLab.Program.Main(args);
                        break;
                    case "2":
                        ProducerConsumerTasks.Program.Main(args);
                        break;
                    case "3":
                        DeadlockLab.Program.Main(args);
                        break;
                    case "4":
                        DeadlockFixed.Program.Main(args);
                        break;
                    case "5":
                        DeadlockTimeout.Program.Main(args);
                        break;
                    case "6":
                        MutexCrossProcess.Program.Main(args);
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
// ЧАСТЬ 1. PRODUCER-CONSUMER С СЕМАФОРОМ — ШАБЛОН ИЗ МЕТОДИЧКИ
// (код приведен без изменений; добавлен только ResetState() для
//  повторного запуска из общего меню, логика Producer/Consumer не тронута)
// =====================================================================
namespace ProducerConsumerLab
{
    class Program
    {
        // Общий буфер (ограниченная очередь)
        private static Queue<int> _buffer = new Queue<int>();
        private const int BufferSize = 5;

        // Семафоры для подсчета свободных и занятых мест
        private static SemaphoreSlim _emptySlots = new SemaphoreSlim(BufferSize, BufferSize);
        private static SemaphoreSlim _filledSlots = new SemaphoreSlim(0, BufferSize);

        // Мьютекс для защиты доступа к буферу
        private static Mutex _mutex = new Mutex();

        // Флаг остановки потоков
        private static bool _isRunning = true;

        // Служебный метод для запуска из общего меню (не входит в шаблон из методички).
        // Сбрасывает статическое состояние, чтобы демонстрацию можно было запускать повторно.
        public static void ResetState()
        {
            _buffer = new Queue<int>();
            _emptySlots = new SemaphoreSlim(BufferSize, BufferSize);
            _filledSlots = new SemaphoreSlim(0, BufferSize);
            _mutex = new Mutex();
            _isRunning = true;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Producer-Consumer with Semaphore ===\n");

            // Запускаем 2 производителя и 3 потребителя
            Task producer1 = Task.Run(() => Producer(1));
            Task producer2 = Task.Run(() => Producer(2));
            Task consumer1 = Task.Run(() => Consumer(1));
            Task consumer2 = Task.Run(() => Consumer(2));
            Task consumer3 = Task.Run(() => Consumer(3));

            // Работаем 5 секунд
            Thread.Sleep(5000);
            _isRunning = false;

            // Ждем завершения всех задач
            Task.WaitAll(producer1, producer2, consumer1, consumer2, consumer3);

            Console.WriteLine("\nВсе потоки завершены. Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Метод производителя
        static async Task Producer(int id)
        {
            Random rand = new Random(id);
            while (_isRunning)
            {
                // Ждем, пока освободится место в буфере
                await _emptySlots.WaitAsync();

                // Захватываем мьютекс для безопасной работы с буфером
                _mutex.WaitOne();
                try
                {
                    int item = rand.Next(1, 100);
                    _buffer.Enqueue(item);
                    Console.WriteLine($"[Producer {id}] Добавлен элемент: {item}. В буфере: {_buffer.Count} элементов.");
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                // Увеличиваем счетчик занятых мест
                _filledSlots.Release();

                // Имитация времени на производство
                await Task.Delay(rand.Next(200, 600));
            }
            Console.WriteLine($"[Producer {id}] Завершен.");
        }

        // Метод потребителя
        static async Task Consumer(int id)
        {
            Random rand = new Random(id + 10);
            while (_isRunning)
            {
                // Ждем, пока появится хотя бы один элемент в буфере
                await _filledSlots.WaitAsync();

                // Захватываем мьютекс
                _mutex.WaitOne();
                int item;
                try
                {
                    item = _buffer.Dequeue();
                    Console.WriteLine($"[Consumer {id}] Извлечен элемент: {item}. В буфере: {_buffer.Count} элементов.");
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                // Увеличиваем счетчик свободных мест
                _emptySlots.Release();

                // Имитация обработки данных
                await Task.Delay(rand.Next(300, 800));
            }
            Console.WriteLine($"[Consumer {id}] Завершен.");
        }
    }
}

// =====================================================================
// ЗАДАНИЯ 2-4. PRODUCER-CONSUMER: ВАРИАЦИИ РАЗМЕРА БУФЕРА, ЧИСЛА
// ПОТОКОВ И ТИПА ХРАНИМЫХ ДАННЫХ (DataItem)
// =====================================================================
namespace ProducerConsumerTasks
{
    // Усложненное задание (пункт 4): объект с полями для хранения в буфере
    class DataItem
    {
        public int Id;
        public string Payload = string.Empty;
    }

    class Program
    {
        // Общий буфер (ограниченная очередь)
        private static Queue<int> _buffer = new Queue<int>();
        private static Queue<DataItem> _objectBuffer = new Queue<DataItem>();

        // Текущий размер буфера (задание 2: 2 / 5 / 10)
        private static int BufferSize = 5;

        // Семафоры для подсчета свободных и занятых мест (инициализируются в RunDemo)
        private static SemaphoreSlim _emptySlots = new SemaphoreSlim(5, 5);
        private static SemaphoreSlim _filledSlots = new SemaphoreSlim(0, 5);

        // Мьютекс для защиты доступа к буферу
        private static Mutex _mutex = new Mutex();

        // Флаг остановки потоков
        private static bool _isRunning = true;

        // Режим работы с объектами (задание 4)
        private static bool _useDataItems = false;

        public static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=== Задания 2-4: Producer-Consumer ===");
                Console.WriteLine("1. BufferSize = 2  (2 producers, 3 consumers)");
                Console.WriteLine("2. BufferSize = 5  (2 producers, 3 consumers)");
                Console.WriteLine("3. BufferSize = 10 (2 producers, 3 consumers)");
                Console.WriteLine("4. BufferSize = 5, 3 producers + 4 consumers (задание 3)");
                Console.WriteLine("5. BufferSize = 5, буфер объектов DataItem (задание 4)");
                Console.WriteLine("0. Назад");
                Console.Write("\nВыберите пункт: ");

                string? choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        RunDemo(bufferSize: 2, producers: 2, consumers: 3, useDataItems: false);
                        break;
                    case "2":
                        RunDemo(bufferSize: 5, producers: 2, consumers: 3, useDataItems: false);
                        break;
                    case "3":
                        RunDemo(bufferSize: 10, producers: 2, consumers: 3, useDataItems: false);
                        break;
                    case "4":
                        RunDemo(bufferSize: 5, producers: 3, consumers: 4, useDataItems: false);
                        break;
                    case "5":
                        RunDemo(bufferSize: 5, producers: 2, consumers: 3, useDataItems: true);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Неверный выбор.");
                        break;
                }
            }
        }

        // Запуск демонстрации с заданными параметрами:
        // задание 2 — размер буфера, задание 3 — число потоков, задание 4 — объекты в буфере
        static void RunDemo(int bufferSize, int producers, int consumers, bool useDataItems)
        {
            // Сброс состояния перед новым запуском
            _buffer = new Queue<int>();
            _objectBuffer = new Queue<DataItem>();
            BufferSize = bufferSize;
            _useDataItems = useDataItems;
            _emptySlots = new SemaphoreSlim(BufferSize, BufferSize);
            _filledSlots = new SemaphoreSlim(0, BufferSize);
            _mutex = new Mutex();
            _isRunning = true;

            Console.WriteLine($"--- BufferSize = {BufferSize}, producers = {producers}, consumers = {consumers}" +
                              (useDataItems ? ", режим DataItem" : "") + " ---\n");

            var tasks = new List<Task>();

            // Производители
            for (int i = 1; i <= producers; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() => Producer(id)));
            }

            // Потребители
            for (int i = 1; i <= consumers; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() => Consumer(id)));
            }

            // Работаем 5 секунд
            Thread.Sleep(5000);
            _isRunning = false;

            // Ждем завершения всех задач
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"\nВсе потоки завершены (BufferSize = {BufferSize}).\n");
        }

        // Метод производителя
        static async Task Producer(int id)
        {
            Random rand = new Random(id);
            while (_isRunning)
            {
                // Ждем, пока освободится место в буфере.
                // Ожидание с таймаутом: если место не освободилось, повторяем
                // цикл и проверяем флаг остановки (защита от зависания при завершении).
                if (!await _emptySlots.WaitAsync(200))
                {
                    continue;
                }

                // Захватываем мьютекс для безопасной работы с буфером
                _mutex.WaitOne();
                try
                {
                    if (_useDataItems)
                    {
                        // Задание 4: кладем в буфер объект DataItem
                        DataItem item = new DataItem
                        {
                            Id = rand.Next(1, 100),
                            Payload = $"payload-producer{id}-{rand.Next(1000, 9999)}"
                        };
                        _objectBuffer.Enqueue(item);
                        Console.WriteLine($"[Producer {id}] Добавлен объект: Id={item.Id}, Payload={item.Payload}. В буфере: {_objectBuffer.Count} объектов.");
                    }
                    else
                    {
                        int item = rand.Next(1, 100);
                        _buffer.Enqueue(item);
                        Console.WriteLine($"[Producer {id}] Добавлен элемент: {item}. В буфере: {_buffer.Count} элементов.");
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                // Увеличиваем счетчик занятых мест
                _filledSlots.Release();

                // Имитация времени на производство
                await Task.Delay(rand.Next(200, 600));
            }
            Console.WriteLine($"[Producer {id}] Завершен.");
        }

        // Метод потребителя
        static async Task Consumer(int id)
        {
            Random rand = new Random(id + 10);
            while (_isRunning)
            {
                // Ждем, пока появится хотя бы один элемент в буфере.
                // Ожидание с таймаутом: если элемент не появился, повторяем
                // цикл и проверяем флаг остановки (защита от зависания при завершении).
                if (!await _filledSlots.WaitAsync(200))
                {
                    continue;
                }

                // Захватываем мьютекс
                _mutex.WaitOne();
                try
                {
                    if (_useDataItems)
                    {
                        // Задание 4: извлекаем объект DataItem
                        DataItem item = _objectBuffer.Dequeue();
                        Console.WriteLine($"[Consumer {id}] Извлечен объект: Id={item.Id}, Payload={item.Payload}. В буфере: {_objectBuffer.Count} объектов.");
                    }
                    else
                    {
                        int item = _buffer.Dequeue();
                        Console.WriteLine($"[Consumer {id}] Извлечен элемент: {item}. В буфере: {_buffer.Count} элементов.");
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                // Увеличиваем счетчик свободных мест
                _emptySlots.Release();

                // Имитация обработки данных
                await Task.Delay(rand.Next(300, 800));
            }
            Console.WriteLine($"[Consumer {id}] Завершен.");
        }
    }
}

// =====================================================================
// ЧАСТЬ 2. ИСКУССТВЕННЫЙ DEADLOCK — ШАБЛОН ИЗ МЕТОДИЧКИ (без изменений)
// Для диагностики: F5 -> дождаться зависания -> Ctrl+Alt+Break ->
// Debug -> Windows -> Parallel Stacks / Threads / Call Stack
// =====================================================================
namespace DeadlockLab
{
    class Program
    {
        // Два объекта для блокировки
        private static object _lockA = new object();
        private static object _lockB = new object();

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Демонстрация Deadlock ===\n");

            // Запускаем два потока, которые захватывают блокировки в разном порядке
            Task task1 = Task.Run(() => Thread1());
            Task task2 = Task.Run(() => Thread2());

            Console.WriteLine("Ожидание завершения потоков... (программа зависнет)");
            Console.WriteLine("Для диагностики откройте окно 'Parallel Stacks' в VS.");

            Task.WaitAll(task1, task2); // Здесь программа зависнет навсегда

            Console.WriteLine("Программа завершена (это сообщение не будет выведено)");
        }

        static void Thread1()
        {
            lock (_lockA)
            {
                Console.WriteLine("Thread1: захватил lockA");
                Thread.Sleep(1000); // Имитация работы

                lock (_lockB)
                {
                    Console.WriteLine("Thread1: захватил lockB");
                }
            }
        }

        static void Thread2()
        {
            lock (_lockB)
            {
                Console.WriteLine("Thread2: захватил lockB");
                Thread.Sleep(1000); // Имитация работы

                lock (_lockA)
                {
                    Console.WriteLine("Thread2: захватил lockA");
                }
            }
        }
    }
}

// =====================================================================
// ЧАСТЬ 2, ЗАДАНИЕ 2. ИСПРАВЛЕННЫЙ DEADLOCK:
// ОДИНАКОВЫЙ ПОРЯДОК ЗАХВАТА БЛОКИРОВОК (A -> B) В ОБОИХ ПОТОКАХ
// =====================================================================
namespace DeadlockFixed
{
    class Program
    {
        // Два объекта для блокировки
        private static object _lockA = new object();
        private static object _lockB = new object();

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Deadlock исправлен: одинаковый порядок захвата блокировок ===\n");

            Task task1 = Task.Run(() => Thread1());
            Task task2 = Task.Run(() => Thread2());

            Console.WriteLine("Ожидание завершения потоков...");
            Task.WaitAll(task1, task2);

            Console.WriteLine("Программа завершена (оба потока корректно отработали)");
            Console.WriteLine("\nНажмите любую клавишу для возврата в меню...");
            Console.ReadKey();
        }

        static void Thread1()
        {
            // Порядок захвата: A -> B
            lock (_lockA)
            {
                Console.WriteLine("Thread1: захватил lockA");
                Thread.Sleep(1000); // Имитация работы

                lock (_lockB)
                {
                    Console.WriteLine("Thread1: захватил lockB");
                }
            }
            Console.WriteLine("Thread1: завершен");
        }

        static void Thread2()
        {
            // Исправление: тот же порядок захвата A -> B, что и в Thread1
            lock (_lockA)
            {
                Console.WriteLine("Thread2: захватил lockA");
                Thread.Sleep(1000); // Имитация работы

                lock (_lockB)
                {
                    Console.WriteLine("Thread2: захватил lockB");
                }
            }
            Console.WriteLine("Thread2: завершен");
        }
    }
}

// =====================================================================
// ЧАСТЬ 2, ЗАДАНИЕ 3. ОБНАРУЖЕНИЕ DEADLOCK ЧЕРЕЗ ТАЙМАУТ:
// Monitor.TryEnter(object, TimeSpan) ВМЕСТО lock
// =====================================================================
namespace DeadlockTimeout
{
    class Program
    {
        // Два объекта для блокировки
        private static object _lockA = new object();
        private static object _lockB = new object();

        // Таймаут ожидания захвата блокировки
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(2);

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Deadlock: обнаружение через Monitor.TryEnter(TimeSpan) ===\n");

            Task task1 = Task.Run(() => Thread1());
            Task task2 = Task.Run(() => Thread2());

            Console.WriteLine("Ожидание завершения потоков...");
            Task.WaitAll(task1, task2);

            Console.WriteLine("\nПрограмма завершена без зависания: таймауты сработали.");
            Console.WriteLine("Нажмите любую клавишу для возврата в меню...");
            Console.ReadKey();
        }

        static void Thread1()
        {
            // Захватываем lockA с таймаутом
            Console.WriteLine("Thread1: пытается захватить lockA...");
            if (!Monitor.TryEnter(_lockA, LockTimeout))
            {
                Console.WriteLine($"Thread1: НЕ УДАЛОСЬ захватить lockA за {LockTimeout.TotalSeconds} сек. Возможен deadlock!");
                return;
            }

            try
            {
                Console.WriteLine("Thread1: захватил lockA");
                Thread.Sleep(1000); // Имитация работы

                // Пытаемся захватить lockB с таймаутом
                Console.WriteLine("Thread1: пытается захватить lockB...");
                if (!Monitor.TryEnter(_lockB, LockTimeout))
                {
                    Console.WriteLine($"Thread1: НЕ УДАЛОСЬ захватить lockB за {LockTimeout.TotalSeconds} сек. ОБНАРУЖЕН deadlock!");
                    return;
                }

                try
                {
                    Console.WriteLine("Thread1: захватил lockB");
                }
                finally
                {
                    Monitor.Exit(_lockB);
                }
            }
            finally
            {
                Monitor.Exit(_lockA);
            }

            Console.WriteLine("Thread1: завершен");
        }

        static void Thread2()
        {
            // Захватывает блокировки в обратном порядке (B -> A), создавая
            // потенциальный deadlock с Thread1, который обнаруживается таймаутом
            Console.WriteLine("Thread2: пытается захватить lockB...");
            if (!Monitor.TryEnter(_lockB, LockTimeout))
            {
                Console.WriteLine($"Thread2: НЕ УДАЛОСЬ захватить lockB за {LockTimeout.TotalSeconds} сек. Возможен deadlock!");
                return;
            }

            try
            {
                Console.WriteLine("Thread2: захватил lockB");
                Thread.Sleep(1000); // Имитация работы

                // Пытаемся захватить lockA с таймаутом
                Console.WriteLine("Thread2: пытается захватить lockA...");
                if (!Monitor.TryEnter(_lockA, LockTimeout))
                {
                    Console.WriteLine($"Thread2: НЕ УДАЛОСЬ захватить lockA за {LockTimeout.TotalSeconds} сек. ОБНАРУЖЕН deadlock!");
                    return;
                }

                try
                {
                    Console.WriteLine("Thread2: захватил lockA");
                }
                finally
                {
                    Monitor.Exit(_lockA);
                }
            }
            finally
            {
                Monitor.Exit(_lockB);
            }

            Console.WriteLine("Thread2: завершен");
        }
    }
}

// =====================================================================
// ЧАСТЬ 3. ИМЕНОВАННЫЙ MUTEX МЕЖДУ ПРОЦЕССАМИ — ШАБЛОН ИЗ МЕТОДИЧКИ
// (без изменений)
// Задание: запустить два экземпляра программы одновременно — второй
// должен выдать сообщение о невозможности захвата мьютекса.
// =====================================================================
namespace MutexCrossProcess
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Именованный Mutex для синхронизации между процессами
            using (Mutex mutex = new Mutex(false, "Global\\MyAppMutex"))
            {
                Console.WriteLine("Попытка захвата глобального мьютекса...");
                if (mutex.WaitOne(TimeSpan.FromSeconds(3)))
                {
                    try
                    {
                        Console.WriteLine("Мьютекс захвачен. Программа выполняется.");
                        Console.WriteLine("Нажмите Enter для освобождения мьютекса...");
                        Console.ReadLine();
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                        Console.WriteLine("Мьютекс освобожден.");
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось захватить мьютекс (возможно, уже запущен другой экземпляр).");
                }
            }
        }
    }
}