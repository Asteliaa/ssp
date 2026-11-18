using OllamaClient.Models;

namespace OllamaClient.Agents;

public class AgentConfig
{
    public string Model { get; set; } = "codellama:7b";
    public string SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
}

public class AgentContext
{
    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}
