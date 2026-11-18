#nullable disable
#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MemoryLab.Templates.InitialLeaks;

public class MemoryLeakExample
{
    // static привязано к классу, а не к объекту, пока есть массивы удалить
    //невозможно -> gb не может их удалить 
    private static List<byte[]> _staticList = new List<byte[]>();
    
    public static void CreateMemoryLeak()
    {
        // Создаем объекты, которые никогда не будут собраны
        for (int i = 0; i < 1000; i++)
        {
            _staticList.Add(new byte[1024 * 10]); // 10KB каждый
        }
    }
}

// по сути мы НАМЕРЕНО удерживаем ненужные
// объекты в памяти, создавая утечку памяти

//---


public class EventLeakExample
{
    public event EventHandler BigEvent;
    
    public void SubscribeLeak()
    {
        // Подписка на событие без отписки
        // Это создаст утечку памяти, так как делегат будет удерживать ссылку на объект
        BigEvent += (sender, e) => 
        {
            var data = new byte[1024 * 100];
        };
    }
}
