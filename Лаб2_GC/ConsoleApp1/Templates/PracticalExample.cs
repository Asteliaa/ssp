using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable disable
#pragma warning disable CS0067

namespace MemoryLab.Templates.Practical;

public class MemoryMonitor : IDisposable
{
    private bool _disposed;
    private List<byte[]> _allocatedMemory = new List<byte[]>();
    private Random _random = new Random();
    
    public void AllocateMemory(int sizeInMB)
    {
        // Выделяем память с возможной утечкой
        var data = new byte[sizeInMB * 1024 * 1024];
        // удерживаем ссылку на объект, чтобы предотвратить сборку мусора
        _allocatedMemory.Add(data);
    }
    
    public void AllocateLOHObjects(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Объекты > 85KB
            var largeObject = new byte[90000 + i];
        }
    }

    // int в object -> упаковка
    public void SimulateBoxing()
    {
        var list = new List<object>();
        for (int i = 0; i < 1000000; i++)
        {
            list.Add(i); // Упаковка
        }
    }
    
    public void PrintMemoryInfo()
    {

        // сколько СБОРОК каждого поколения произошло
        Console.WriteLine($"GC Generation: {GC.GetGeneration(this)}");
        Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Collection Count Gen0: {GC.CollectionCount(0)}");
        Console.WriteLine($"Collection Count Gen1: {GC.CollectionCount(1)}");
        Console.WriteLine($"Collection Count Gen2: {GC.CollectionCount(2)}");
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

// задание 3
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Лабораторная работа: Управление памятью ===");
        
        using (var monitor = new MemoryMonitor())
        {
            // 1. Тестирование базовой памяти
            Console.WriteLine("\n1. Базовое выделение памяти:");
            monitor.AllocateMemory(10);
            monitor.PrintMemoryInfo();
            
            // 2. Тестирование LOH
            Console.WriteLine("\n2. Создание объектов в LOH:");
            monitor.AllocateLOHObjects(100);
            monitor.PrintMemoryInfo();
            
            // 3. Тестирование Boxing
            Console.WriteLine("\n3. Тест упаковки:");
            var stopwatch = Stopwatch.StartNew();
            monitor.SimulateBoxing();
            stopwatch.Stop();
            Console.WriteLine($"Время выполнения с упаковкой: {stopwatch.ElapsedMilliseconds}ms");
            
            // 4. Принудительная сборка
            Console.WriteLine("\n4. После принудительной сборки:");
            monitor.Cleanup();
            monitor.PrintMemoryInfo();
            
            // 5. Оптимизированная версия
            Console.WriteLine("\n5. Оптимизированная версия (без упаковки):");
            stopwatch.Restart();
            var optimizedList = new List<int>();
            for (int i = 0; i < 1000000; i++)
            {
                optimizedList.Add(i);
            }
            stopwatch.Stop();
            Console.WriteLine($"Время выполнения без упаковки: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        Console.WriteLine("\nНажмите любую клавишу для завершения...");
        Console.ReadKey();
    }
}
