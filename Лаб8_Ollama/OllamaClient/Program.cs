using OllamaClient;
using OllamaClient.Agents;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("КЛИЕНТ ЛОКАЛЬНОЙ LLM OLLAMA");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var client = new OllamaApiClient();

        // Проверка запущен ли Ollama
        try
        {
            var models = await client.ListModelsAsync();
            Console.WriteLine("✓ Подключено к Ollama. Доступные модели:");
            foreach (var model in models)
            {
                Console.WriteLine($"  - {model}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine("✗ Ошибка подключения к Ollama.");
            Console.WriteLine($"  {ex.Message}");
            Console.WriteLine("  Убедитесь, что Ollama запущен на localhost:11434");
            return;
        }

        // ============================================
        // ЗАДАНИЕ A: Базовая генерация кода
        // ============================================

        Console.WriteLine("=== ЗАДАНИЕ A: Генерация кода ===");
        Console.WriteLine();

        var prompt = "Напишите C# метод для поиска максимального элемента в массиве";

        Console.WriteLine($"Промпт: {prompt}");
        Console.WriteLine();
        Console.WriteLine("Ответ:");

        await foreach (var token in client.GenerateStreamAsync(
            "codellama:7b",
            prompt,
            "Вы — эксперт-разработчик на C#. Предоставляйте чистый, эффективный код с комментариями."
        ))
        {
            Console.Write(token);
        }
        Console.WriteLine();
        Console.WriteLine();

        // ============================================
        // ЗАДАНИЕ B: Интеллектуальный агент
        // ============================================

        Console.WriteLine("=== ЗАДАНИЕ B: Агент-помощник по коду ===");
        Console.WriteLine();

        var config = new AgentConfig
        {
            Model = "codellama:7b",
            SystemPrompt = @"Вы — эксперт-разработчик на C#.
    Предоставляйте краткий, чистый и эффективный код.
    Всегда включайте комментарии, объясняющие ваш код.
    Предлагайте лучшие практики и паттерны проектирования, где это уместно.",
            Temperature = 0.7
        };

        var assistant = new CodeAssistantAgent(client, config);

        var codePrompt = "Создайте C# класс, реализующий потокобезопасный паттерн Singleton";
        Console.WriteLine($"Пользователь: {codePrompt}");
        Console.WriteLine();

        var response = await assistant.ProcessAsync(codePrompt);
        Console.WriteLine("Ассистент:");
        Console.WriteLine(response);
        Console.WriteLine();

        // ============================================
        // ЗАДАНИЕ C: Потоковая передача с системным промптом
        // ============================================

        Console.WriteLine("=== ЗАДАНИЕ C: Потоковая передача с системным промптом ===");
        Console.WriteLine();

        var systemPrompt = @"Вы — полезный ревьювер кода.
    Анализируйте код на:
    1. Логические ошибки
    2. Проблемы производительности
    3. Нарушения стиля кода
    4. Уязвимости безопасности
    Предоставляйте конструктивную обратную связь.";

        var codeReviewPrompt = @"public int CalculateAverage(int[] numbers) {
        int sum = 0;
        for (int i = 0; i <= numbers.Length; i++) {
            sum += numbers[i];
        }
        return sum / numbers.Length;
    }";

        Console.WriteLine("Код для проверки:");
        Console.WriteLine(codeReviewPrompt);
        Console.WriteLine();

        Console.WriteLine("Ревью:");

        await foreach (var token in client.GenerateStreamAsync(
            "deepseek-coder:6.7b",
            codeReviewPrompt,
            systemPrompt
        ))
        {
            Console.Write(token);
        }
        Console.WriteLine();

        // ============================================
        // ЗАДАНИЕ D: Конвейер агентов
        // ============================================

        Console.WriteLine("=== ЗАДАНИЕ D: Конвейер агентов ===");
        Console.WriteLine();

        var pipelineConfig = new AgentConfig
        {
            Model = "codellama:7b",
            SystemPrompt = "Вы — эксперт по генерации кода."
        };

        var reviewConfig = new AgentConfig
        {
            Model = "deepseek-coder:6.7b",
            SystemPrompt = "Вы — эксперт по ревью кода."
        };

        var pipeline = new AgentPipeline();
        pipeline.AddAgent(new CodeAssistantAgent(client, pipelineConfig));
        pipeline.AddAgent(new CodeReviewAgent(client, reviewConfig));

        var pipelinePrompt = """
        Напишите C# метод для вычисления последовательности Фибоначчи с использованием рекурсии.
        Включите правильную обработку ошибок и документацию.
        """;

        Console.WriteLine($"Пользователь: {pipelinePrompt}");
        Console.WriteLine();

        var result = await pipeline.ProcessAsync(pipelinePrompt);
        Console.WriteLine("Финальный результат:");
        Console.WriteLine(result);
    }
}
