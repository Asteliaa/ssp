#nullable disable
#pragma warning disable CS0067
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MemoryLab.Templates.Additional;

public class LOHExample
{
    public static void CreateLargeObjects()
    {
        // Объекты > 85KB попадают в LOH
        for (int i = 0; i < 100; i++)
        {
            var largeArray = new byte[100000]; // ~100KB
            // Используем largeArray
        }
    }
}

public class BoxingExample
{
    private List<object> _boxedItems = new List<object>();
    private List<int> _unboxedItems = new List<int>();
    
    public void BoxingOperation()
    {
        // Упаковка - создает дополнительные объекты в куче
        for (int i = 0; i < 1000000; i++)
        {
            _boxedItems.Add(i); // int упаковывается в object
        }
    }
    
    public void UnboxedOperation()
    {
        // Без упаковки - эффективнее
        for (int i = 0; i < 1000000; i++)
        {
            _unboxedItems.Add(i);
        }
    }
}

public class UnoptimizedCode
{
    private ArrayList _data = new ArrayList();
    
    public void AddData(int value)
    {
        _data.Add(value); // Упаковка!
    }
    
    public int GetSum()
    {
        int sum = 0;
        foreach (object item in _data) // Распаковка!
        {
            sum += (int)item;
        }
        return sum;
    }
}

public class BuggyCode
{
    private static List<IDisposable> _cache = new List<IDisposable>();
    private static event EventHandler _globalEvent;
    private byte[] _data = new byte[1024 * 1024];
    
    public BuggyCode()
    {
        _globalEvent += OnEvent;
        _cache.Add(new MemoryStream(1024));
    }
    
    private void OnEvent(object sender, EventArgs e)
    {
        var temp = new byte[1024 * 100];
    }
    
    public void SubscribeAll()
    {
        for (int i = 0; i < 1000; i++)
        {
            new BuggyCode();
        }
    }
}

public static class BoxingBasics
{
    public static void Boxing()
    {
        // Значимый тип (хранится на стеке)
        int number = 42;

        // Упаковка - создает объект в куче
        object boxedNumber = number; // Boxing!
    }

    public static void Unboxing()
    {
        object boxedNumber = 42; // Упаковка

        // Распаковка - требуется явное приведение
        int unboxedNumber = (int)boxedNumber; // Unboxing!
    }
}
