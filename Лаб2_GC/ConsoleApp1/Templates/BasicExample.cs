using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable disable
#pragma warning disable CS0067

namespace MemoryLab.Templates.Basic;

public class MemoryLeakExample
{
    private static List<byte[]> _staticList = new List<byte[]>();
    
    public static void CreateMemoryLeak()
    {
        for (int i = 0; i < 1000; i++)
        {
            _staticList.Add(new byte[1024 * 10]);
        }
    }

    public static void ClearMemoryLeak()
    {
        // удаляем все ссылки
        _staticList.Clear();
        // вызываем сборку мусора
        GC.Collect();
        // ждем завершения финализаторов
        GC.WaitForPendingFinalizers();
    }
}


public class EventLeakExample
{
    public event EventHandler BigEvent;
    
    public void SubscribeLeak()
    {
        BigEvent += (sender, e) => 
        {
            var data = new byte[1024 * 100];
        };
    }
}

public class MemoryMonitor
{
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
        {
            list.Add(i);
        }
    }
    
    public void PrintMemoryInfo(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine($"\n=== {message} ===");
        
        Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
        
        using (Process process = Process.GetCurrentProcess())
        {
            Console.WriteLine($"Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
        }
    }
    
    public void Cleanup()
    {
        _allocatedMemory.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Лабораторная работа: Управление памятью ===");
        var monitor = new MemoryMonitor();
        
        // 1. Тест памяти и LOH
        Console.WriteLine("\n1. Выделение памяти:");
        monitor.AllocateMemory(10);
        monitor.AllocateLOHObjects(100);
        monitor.PrintMemoryInfo("После выделения");
        
        // 2. Тест утечек
        Console.WriteLine("\n2. Тест утечек:");
        MemoryLeakExample.CreateMemoryLeak();
        var eventExample = new EventLeakExample();
        for (int i = 0; i < 100; i++)
            eventExample.SubscribeLeak();
        monitor.PrintMemoryInfo("После утечек");
        
        // 3. Тест Boxing
        Console.WriteLine("\n3. Тест упаковки:");
        var sw = Stopwatch.StartNew();
        monitor.SimulateBoxing(1000000);
        sw.Stop();
        Console.WriteLine($"Время с упаковкой: {sw.ElapsedMilliseconds}ms");
        
        // Без упаковки
        sw.Restart();
        var optimizedList = new List<int>();
        for (int i = 0; i < 1000000; i++)
            optimizedList.Add(i);
        sw.Stop();
        Console.WriteLine($"Время без упаковки: {sw.ElapsedMilliseconds}ms");
        
        // 4. Очистка
        Console.WriteLine("\n4. После очистки:");
        MemoryLeakExample.ClearMemoryLeak();
        monitor.Cleanup();
        monitor.PrintMemoryInfo("Очищено");
        
        // 5. Поколения объектов
        Console.WriteLine("\n5. Поколения объектов:");
        var obj = new object();
        Console.WriteLine($"Начальное поколение: {GC.GetGeneration(obj)}");
        GC.Collect();
        Console.WriteLine($"После GC: {GC.GetGeneration(obj)}");
        
        Console.WriteLine("\nНажмите любую клавишу...");
        Console.ReadKey();
    }
}
