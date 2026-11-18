namespace OllamaClient.Agents;

public class AgentPipeline
{
    private readonly List<Agent> _agents = new();

    public void AddAgent(Agent agent) => _agents.Add(agent);

    public async Task<string> ProcessAsync(string input)
    {
        var result = input;
        var step = 1;

        foreach (var agent in _agents)
        {
            Console.WriteLine($"Агент {step}/{_agents.Count} обрабатывает...");
            result = await agent.ProcessAsync(result);
            Console.WriteLine($"Агент {step} завершен.");
            step++;
        }

        return result;
    }
}
