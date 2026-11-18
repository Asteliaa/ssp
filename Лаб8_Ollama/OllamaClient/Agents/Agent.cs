using System.Text;
using OllamaClient.Models;

namespace OllamaClient.Agents;

public abstract class Agent
{
    protected readonly OllamaApiClient _client;
    protected readonly AgentConfig _config;
    protected readonly AgentContext _context;

    public Agent(OllamaApiClient client, AgentConfig config)
    {
        _client = client;
        _config = config;
        _context = new AgentContext();

        // Инициализация системным промптом
        if (!string.IsNullOrEmpty(config.SystemPrompt))
        {
            _context.Messages.Add(new ChatMessage("system", config.SystemPrompt));
        }
    }

    public abstract Task<string> ProcessAsync(string input);
}

public class CodeAssistantAgent : Agent
{
    private readonly string _language;

    public CodeAssistantAgent(
        OllamaApiClient client,
        AgentConfig config,
        string language = "C#")
        : base(client, config)
    {
        _language = language;
    }

    public override async Task<string> ProcessAsync(string input)
    {
        // Добавление сообщения пользователя в контекст
        _context.Messages.Add(new ChatMessage("user", input));

        // Формирование полного промпта с контекстом
        var prompt = BuildPrompt(input);

        // Генерация ответа
        var response = await _client.GenerateAsync(
            _config.Model,
            prompt,
            _config.SystemPrompt
        );

        // Добавление ответа ассистента в контекст
        _context.Messages.Add(new ChatMessage("assistant", response));

        return response;
    }

    private string BuildPrompt(string input)
    {
        var sb = new StringBuilder();

        // Добавление системного промпта
        if (!string.IsNullOrEmpty(_config.SystemPrompt))
        {
            sb.AppendLine($"System: {_config.SystemPrompt}");
            sb.AppendLine();
        }

        // Добавление истории разговора
        foreach (var msg in _context.Messages)
        {
            if (msg.Role != "system")
            {
                sb.AppendLine($"{msg.Role}: {msg.Content}");
            }
        }

        return sb.ToString();
    }
}

public class CodeReviewAgent : Agent
{
    public CodeReviewAgent(OllamaApiClient client, AgentConfig config)
        : base(client, config)
    {
    }

    public override async Task<string> ProcessAsync(string input)
    {
        var prompt = BuildReviewPrompt(input);

        return await _client.GenerateAsync(
            _config.Model,
            prompt,
            _config.SystemPrompt
        );
    }

    private string BuildReviewPrompt(string code)
    {
        return $"""
        Пожалуйста, проверьте следующий код и предоставьте:
        1. Оценку качества кода
        2. Потенциальные ошибки или проблемы
        3. Предложения по производительности
        4. Проблемы безопасности
        5. Улучшения по лучшим практикам

        Код:
        {code}
        """;
    }
}
