using System.Reflection;

namespace MemoryLab;

internal static class LabLauncher
{
    internal static bool TryRun(string[] args)
    {
        if (args.Length == 0 && !Console.IsInputRedirected)
        {
            Console.WriteLine("=== Лабораторная работа 2 ===");
            Console.WriteLine("1. Дословный пример: статические и экземплярные ссылки");
            Console.WriteLine("2. Дословный пример задания 1 (как на скриншоте)");
            Console.WriteLine("3. Дословные задания 2 и 3: MemoryMonitor : IDisposable");
            Console.WriteLine("4. Исследования заданий 1–7 и автоматические проверки");
            Console.WriteLine("5. Исследования с паузами для Memory Profiler");
            Console.WriteLine("6. Дополнительные шаблоны: LOH, boxing, ArrayList");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите пункт (Enter = 4): ");
            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1": RunTemplate("static"); break;
                case "2": RunTemplate("basic"); return true;
                case "3": RunTemplate("practical"); return true;
                case "5": Program.RunProfile(); return true;
                case "6": RunTemplate("extras"); break;
                case "0": return true;
                case "": case "4": case null: return false;
                default:
                    Console.Error.WriteLine("Неизвестный пункт меню.");
                    Environment.ExitCode = 1;
                    return true;
            }
            Console.WriteLine("Нажмите Enter для завершения.");
            Console.ReadLine();
            return true;
        }

        if (args.Length == 0 || args.SequenceEqual(new[] { "--all" }) ||
            args.SequenceEqual(new[] { "--profile" }))
            return false;

        if (args.Length == 2 && args[0] == "--template")
        {
            RunTemplate(args[1]);
            return true;
        }

        if (args.SequenceEqual(new[] { "--verify-templates" }))
        {
            VerifyTemplates();
            return true;
        }

        Console.WriteLine("Запуск: --all | --profile | --template static|basic|practical|initial|extras | --verify-templates");
        Console.WriteLine("Без аргументов: меню. --profile: паузы для снимков памяти.");
        if (!args.SequenceEqual(new[] { "--help" })) Environment.ExitCode = 1;
        return true;
    }

    private static void RunTemplate(string name)
    {
        if ((name == "basic" || name == "practical") && Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Исходный Main содержит Console.ReadKey(). Запустите его в интерактивной консоли.");
            Environment.ExitCode = 1;
            return;
        }

        switch (name)
        {
            case "static": InvokeMain(typeof(Templates.StaticReferences.StaticReferenceDemo).Assembly,
                "MemoryLab.Templates.StaticReferences.Program"); break;
            case "basic": InvokeMain(typeof(Templates.Basic.MemoryMonitor).Assembly,
                "MemoryLab.Templates.Basic.Program"); break;
            case "practical": InvokeMain(typeof(Templates.Practical.MemoryMonitor).Assembly,
                "MemoryLab.Templates.Practical.Program"); break;
            case "initial":
                Templates.InitialLeaks.MemoryLeakExample.CreateMemoryLeak();
                var publisher = new Templates.InitialLeaks.EventLeakExample();
                for (int i = 0; i < 100; i++) publisher.SubscribeLeak();
                Program.FullCollection();
                Console.WriteLine($"Исходный шаблон: {Program.ToMiB(GC.GetTotalMemory(false)):F3} MiB после GC.");
                Console.WriteLine("1000 массивов остаются в static до завершения процесса. Событие не вызывалось.");
                GC.KeepAlive(publisher);
                break;
            case "extras":
                Templates.Additional.LOHExample.CreateLargeObjects();
                var boxing = new Templates.Additional.BoxingExample();
                boxing.BoxingOperation();
                boxing.UnboxedOperation();
                Templates.Additional.BoxingBasics.Boxing();
                Templates.Additional.BoxingBasics.Unboxing();
                var original = new Templates.Additional.UnoptimizedCode();
                for (int i = 1; i <= 100; i++) original.AddData(i);
                Program.Require(original.GetSum() == 5050, "Исходный ArrayList: сумма 1..100 = 5050");
                GC.KeepAlive(boxing);
                Console.WriteLine("Дополнительные исходные примеры выполнены.");
                Console.WriteLine("BuggyCode сохранен, но SubscribeAll не вызывается: он удерживает более 1 GiB.");
                break;
            default:
                Console.Error.WriteLine("Неизвестный шаблон. Используйте --help.");
                Environment.ExitCode = 1;
                break;
        }
    }

    private static void InvokeMain(Assembly assembly, string typeName)
    {
        // Reflection позволяет вызвать private Main, не меняя текст преподавателя.
        MethodInfo main = assembly.GetType(typeName, throwOnError: true)!
            .GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
        main.Invoke(null, main.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() });
    }

    private static void VerifyTemplates()
    {
        RunTemplate("static");
        var basic = new Templates.Basic.MemoryMonitor();
        basic.AllocateMemory(10);
        basic.AllocateLOHObjects(100);
        basic.SimulateBoxing();
        Templates.Basic.MemoryLeakExample.CreateMemoryLeak();
        var publisher = new Templates.Basic.EventLeakExample();
        for (int i = 0; i < 100; i++) publisher.SubscribeLeak();
        basic.PrintMemoryInfo("Проверка исходного базового монитора");
        Templates.Basic.MemoryLeakExample.ClearMemoryLeak();
        basic.Cleanup();
        GC.KeepAlive(publisher);

        using (var practical = new Templates.Practical.MemoryMonitor())
        {
            practical.AllocateMemory(10);
            practical.AllocateLOHObjects(100);
            practical.SimulateBoxing();
            practical.PrintMemoryInfo();
            practical.Cleanup();
            practical.Dispose();
            practical.Dispose();
        }
        RunTemplate("extras");
        Console.WriteLine("Smoke-проверка API шаблонов завершена; интерактивные Main с ReadKey здесь не вызываются.");
    }
}
