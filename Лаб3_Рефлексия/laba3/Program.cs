#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LabReflection
{
    // Класс для исследования из примера методички
    public class Product
    {
        // Поля
        private int _id;
        private string _name;
        protected double _price;
        public static int TotalProducts = 0;

        // Свойства
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Category { get; private set; }

        // Конструкторы
        public Product()
        {
            _id = 0;
            _name = "Unknown";
            Category = "Default";
            TotalProducts++;
        }

        public Product(int id, string name, double price)
        {
            _id = id;
            _name = name;
            _price = price;
            Id = id;
            Name = name;
            Price = price;
            Category = "Default";
            TotalProducts++;
        }

        // Методы
        public void DisplayInfo()
        {
            Console.WriteLine($"Product: {_name}, Price: {_price}");
        }

        public double ApplyDiscount(double percent)
        {
            return _price * (1 - percent / 100);
        }

        private void ValidatePrice()
        {
            if (_price < 0) _price = 0;
        }

        protected void UpdateCategory(string newCategory)
        {
            Category = newCategory;
        }

        public static int GetTotalCount()
        {
            return TotalProducts;
        }
    }

    // Класс с приватными полями и свойствами для задания 1
    public class ReflectionTarget
    {
        private int _age;
        private string _name;
        private double _rating;

        public ReflectionTarget()
        {
            _age = 18;
            _name = "Unknown";
            _rating = 0;
        }

        public ReflectionTarget(int age, string name, double rating)
        {
            Age = age;
            Name = name;
            Rating = rating;
        }

        public int Age
        {
            get => _age;
            set => _age = value < 0 ? 0 : value;
        }

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }

        public double Rating
        {
            get => _rating;
            set => _rating = Math.Clamp(value, 0, 5);
        }

        public string GetDescription()
        {
            return $"{Name}, возраст: {Age}, рейтинг: {Rating}";
        }

        private bool IsAdult()
        {
            return _age >= 18;
        }

        private static string GetClassMarker()
        {
            return "ReflectionTarget";
        }
    }

    // Чёрный ящик для заданий 4-6
    public class SecretCalculator
    {
        private int _secretValue;

        public SecretCalculator(int secretValue)
        {
            _secretValue = secretValue;
        }

        public int ReadValue()
        {
            return _secretValue;
        }

        private int Calculate(int a, int b)
        {
            return (_secretValue + a) * b;
        }

        private void Reset()
        {
            _secretValue = 0;
        }
    }

    // Класс-калькулятор для демонстрации позднего связывания
    public class DynamicCalculator
    {
        private int _result = 0;

        public DynamicCalculator()
        {
            Console.WriteLine("  [Конструктор DynamicCalculator без параметров]");
        }

        public DynamicCalculator(int initialValue)
        {
            _result = initialValue;
            Console.WriteLine($"  [Конструктор с параметром: начальное значение = {initialValue}]");
        }

        public int Add(int a, int b)
        {
            int res = a + b;
            Console.WriteLine($"  Add({a}, {b}) = {res}");
            return res;
        }

        public double Add(double a, double b)
        {
            double res = a + b;
            Console.WriteLine($"  Add({a}, {b}) = {res}");
            return res;
        }

        public int Multiply(int a, int b)
        {
            int res = a * b;
            Console.WriteLine($"  Multiply({a}, {b}) = {res}");
            return res;
        }

        private void LogOperation(string operation)
        {
            Console.WriteLine($"  [LOG] Выполнена операция: {operation}");
        }

        public int Result => _result;

        public void SetResult(int value)
        {
            _result = value;
            Console.WriteLine($"  Установлен результат: {_result}");
        }

        public static string GetVersion()
        {
            return "DynamicCalculator v1.0";
        }
    }

    // ============ АТРИБУТЫ ============

    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public string Name { get; }
        public PluginAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PluginMethodAttribute : Attribute
    {
        public string Description { get; }
        public PluginMethodAttribute(string description) => Description = description;
    }

    // ============ ИНТЕРФЕЙС ПЛАГИНА ============

    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        void Execute();
    }

    public interface IParameterizedPlugin : IPlugin
    {
        void ExecuteWithParams(Dictionary<string, object> parameters);
    }

    // ============ ПЛАГИНЫ ============

    [Plugin("Calculator")]
    public class CalculatorPlugin : IPlugin
    {
        public string Name => "Calculator";
        public string Version => "1.0.0";

        [PluginMethod("Запуск калькулятора")]
        public void Execute()
        {
            Console.WriteLine("Calculator plugin loaded");
        }

        [PluginMethod("Сложение двух чисел")]
        public double Add(double a, double b) => a + b;

        [PluginMethod("Вычитание двух чисел")]
        public double Subtract(double a, double b) => a - b;
    }

    [Plugin("Logger")]
    public class LoggerPlugin : IParameterizedPlugin
    {
        public string Name => "Logger";
        public string Version => "1.0.0";

        [PluginMethod("Запуск логгера")]
        public void Execute()
        {
            Console.WriteLine("Logger plugin executed");
        }

        [PluginMethod("Запись сообщения")]
        public void ExecuteWithParams(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("message", out var msg))
            {
                Console.WriteLine($"[LOG] {msg}");
            }
        }
    }

    // Основной класс для загрузки плагинов
    public class PluginManager
    {
        private readonly List<IPlugin> _plugins = new List<IPlugin>();
        private readonly string _pluginsPath;

        public PluginManager(string pluginsPath = null)
        {
            _pluginsPath = pluginsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            Directory.CreateDirectory(_pluginsPath);
        }

        // Загрузка всех плагинов из директории
        public void LoadPlugins()
        {
            var dllFiles = Directory.GetFiles(_pluginsPath, "*.dll");

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    LoadPluginFromAssembly(dllPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading plugin {dllPath}: {ex.Message}");
                }
            }
        }

        // Загрузка плагина из конкретной сборки
        private void LoadPluginFromAssembly(string assemblyPath)
        {
            // Загрузка сборки
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            LoadPluginTypes(assembly.GetTypes());
        }

        // Загрузка плагинов из текущей сборки позволяет запустить пример без внешних DLL.
        public void LoadPluginsFromAssembly(Assembly assembly)
        {
            LoadPluginTypes(assembly.GetTypes());
        }

        public void LoadConfiguredPlugins(Assembly assembly, string configurationPath)
        {
            string[] configuredTypeNames = File.ReadAllLines(configurationPath)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToArray();

            Type[] types = assembly.GetTypes()
                .Where(type => configuredTypeNames.Contains(type.FullName, StringComparer.Ordinal))
                .ToArray();

            LoadPluginTypes(types);
        }

        private void LoadPluginTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                // Проверка, реализует ли тип IPlugin, и наличие атрибута плагина
                PluginAttribute pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
                if (pluginAttribute != null && typeof(IPlugin).IsAssignableFrom(type) &&
                    !type.IsInterface && !type.IsAbstract)
                {
                    if (_plugins.Any(plugin => plugin.GetType() == type))
                        continue;

                    // Создание экземпляра плагина
                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                    _plugins.Add(plugin);
                    Console.WriteLine($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                }
            }
        }

        // Получение всех загруженных плагинов
        public IEnumerable<IPlugin> GetPlugins() => _plugins;

        // Получение плагина по имени
        public IPlugin GetPlugin(string name)
        {
            return _plugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Выполнение плагина с параметрами через рефлексию
        public void ExecutePluginMethod(IPlugin plugin, string methodName, object[] parameters)
        {
            Type pluginType = plugin.GetType();

            // Поиск метода
            MethodInfo method = pluginType.GetMethod(methodName);

            if (method != null)
            {
                // Вызов метода
                var result = method.Invoke(plugin, parameters);
                Console.WriteLine($"Method {methodName} executed. Result: {result ?? "null"}");
            }
            else
            {
                Console.WriteLine($"Method {methodName} not found in plugin {plugin.Name}");
            }
        }

        // Вызов метода с параметрами через рефлексию с проверкой типов
        public T ExecutePluginMethod<T>(IPlugin plugin, string methodName, object[] parameters)
        {
            Type pluginType = plugin.GetType();
            MethodInfo method = pluginType.GetMethod(methodName);

            if (method != null && method.ReturnType == typeof(T))
            {
                return (T)method.Invoke(plugin, parameters);
            }

            return default;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Task1AndTask2();
            Task3();
            Task4AndTask5();
            Task6();
            Task7();

            Console.WriteLine("\n=== ЛАБОРАТОРНАЯ РАБОТА ЗАВЕРШЕНА ===");
        }

        static void Task1AndTask2()
        {
            Console.WriteLine("=== ЗАДАНИЕ 1: КЛАСС С ПРИВАТНЫМИ ПОЛЯМИ ===\n");

            ReflectionTarget target = new ReflectionTarget(-5, "", 7.5);
            Console.WriteLine($"  После валидации свойств: {target.GetDescription()}");
                
            Console.WriteLine("\n=== ЗАДАНИЕ 2: ИНСПЕКЦИЯ МЕТОДОВ ===\n");
            Type type = typeof(ReflectionTarget);

            MethodInfo[] publicMethods = type.GetMethods();
            MethodInfo[] allMethods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static);

            Console.WriteLine($"  Публичных методов (включая Object): {publicMethods.Length}");
            Console.WriteLine($"  Всех методов: {allMethods.Length}");
            Console.WriteLine("  Методы без аксессоров свойств:");

            foreach (MethodInfo method in allMethods.Where(m =>
                         !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")))
            {
                string access = method.IsPublic ? "public" :
                    method.IsPrivate ? "private" :
                    method.IsFamily ? "protected" : "other";
                string modifiers = method.IsStatic ? $"{access} static" : access;
                var parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  {modifiers} {method.ReturnType.Name} {method.Name}({parameters})");
            }
        }

        static void Task3()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 3: ДИНАМИЧЕСКОЕ СОЗДАНИЕ ===\n");

            Type targetType = typeof(ReflectionTarget);

            // Вызов конструктора без параметров
            object target = Activator.CreateInstance(targetType);
            Console.WriteLine($"  Экземпляр без параметров создан: {target.GetType().Name}");

            PropertyInfo nameProperty = targetType.GetProperty("Name");
            PropertyInfo ageProperty = targetType.GetProperty("Age");
            PropertyInfo ratingProperty = targetType.GetProperty("Rating");
            nameProperty.SetValue(target, "Reflection object");
            ageProperty.SetValue(target, 25);
            ratingProperty.SetValue(target, 4.8);
            Console.WriteLine($"  Значения через Reflection: {((ReflectionTarget)target).GetDescription()}");

            // Вызов конструктора с параметрами
            ConstructorInfo constructor = targetType.GetConstructor(
                new[] { typeof(int), typeof(string), typeof(double) });
            object targetWithParameters = constructor.Invoke(new object[] { 30, "Dynamic object", 4.5 });
            Console.WriteLine($"  Экземпляр с параметрами: {((ReflectionTarget)targetWithParameters).GetDescription()}");
        }

        static void Task4AndTask5()
        {
            Console.WriteLine("\n=== ЗАДАНИЯ 4-5: ПРИВАТНОЕ ПОЛЕ ===\n");

            SecretCalculator calculator = new SecretCalculator(10);
            Type type = typeof(SecretCalculator);
            FieldInfo secretField = type.GetField("_secretValue",
                BindingFlags.NonPublic | BindingFlags.Instance);

            try
            {
                if (secretField == null)
                    throw new MissingFieldException("Поле _secretValue не найдено");

                Console.WriteLine($"  До изменения через публичный метод: {calculator.ReadValue()}");
                secretField.SetValue(calculator, 25);
                Console.WriteLine($"  После изменения через публичный метод: {calculator.ReadValue()}");
                Console.WriteLine("  Инкапсуляция изменена с помощью FieldInfo.SetValue().");
            }
            catch (FieldAccessException ex)
            {
                Console.WriteLine($"  Недостаточно прав для доступа к полю: {ex.Message}");
            }
            catch (MissingFieldException ex)
            {
                Console.WriteLine($"  Ошибка поиска поля: {ex.Message}");
            }
        }

        static void Task6()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 6: ПРИВАТНЫЕ МЕТОДЫ ===\n");

            SecretCalculator calculator = new SecretCalculator(5);
            Type type = calculator.GetType();

            MethodInfo calculateMethod = type.GetMethod("Calculate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (calculateMethod != null)
            {
                object result = calculateMethod.Invoke(calculator, new object[] { 3, 4 });
                Console.WriteLine($"  Calculate через MethodInfo.Invoke(3, 4): {result}");
            }

            MethodInfo resetMethod = type.GetMethod("Reset",
                BindingFlags.NonPublic | BindingFlags.Instance);
            resetMethod?.Invoke(calculator, null);
            Console.WriteLine($"  Reset через MethodInfo.Invoke: {calculator.ReadValue()}");

            SecretCalculator secondCalculator = new SecretCalculator(7);
            MethodInfo secondCalculateMethod = type.GetMethod("Calculate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Func<int, int, int> calculateDelegate =
                (Func<int, int, int>)secondCalculateMethod.CreateDelegate(typeof(Func<int, int, int>), secondCalculator);
            Console.WriteLine($"  Calculate через делегат (2, 3): {calculateDelegate(2, 3)}");
        }

        static void Task7()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 7: СИСТЕМА ПЛАГИНОВ ===\n");

            PluginManager manager = new PluginManager();

            // Внешние DLL загружаются из каталога Plugins по шаблону методички.
            manager.LoadPlugins();
            // Имена типов плагинов считываются из plugins.config.
            manager.LoadConfiguredPlugins(
                Assembly.GetExecutingAssembly(),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins.config"));

            foreach (IPlugin plugin in manager.GetPlugins())
            {
                Type pluginType = plugin.GetType();
                PluginAttribute classAttribute = pluginType.GetCustomAttribute<PluginAttribute>();
                Console.WriteLine($"  Атрибут класса: {classAttribute?.Name ?? "Не указан"}");

                MethodInfo[] methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (method.DeclaringType == typeof(object))
                        continue;

                    PluginMethodAttribute methodAttribute = method.GetCustomAttribute<PluginMethodAttribute>();
                    if (methodAttribute != null)
                        Console.WriteLine($"  Метод {method.Name}: {methodAttribute.Description}");
                }

                plugin.Execute();
            }

            IPlugin calculator = manager.GetPlugin("Calculator");
            if (calculator != null)
            {
                manager.ExecutePluginMethod(calculator, "Add", new object[] { 10.0, 2.5 });
                double result = manager.ExecutePluginMethod<double>(
                    calculator, "Subtract", new object[] { 10.0, 2.5 });
                Console.WriteLine($"  Результат Subtract через generic Invoke: {result}");
            }

            IPlugin logger = manager.GetPlugin("Logger");
            if (logger is IParameterizedPlugin parameterizedLogger)
            {
                parameterizedLogger.ExecuteWithParams(new Dictionary<string, object>
                {
                    ["message"] = "Плагин вызван с параметрами"
                });
            }
        }
    }
}
