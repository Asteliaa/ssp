using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OllamaClient;

public class OllamaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaApiClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Получить список всех установленных моделей
    /// </summary>
    public async Task<List<string>> ListModelsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var models = document.RootElement.GetProperty("models");

        var modelNames = new List<string>();
        foreach (var model in models.EnumerateArray())
        {
            modelNames.Add(model.GetProperty("name").GetString());
        }
        return modelNames;
    }

    /// <summary>
    /// Сгенерировать ответ от модели
    /// </summary>
    public async Task<string> GenerateAsync(
        string model,
        string prompt,
        string systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model,
            prompt,
            system = systemPrompt,
            stream = false,
            options = new { temperature = 0.7 }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/generate",
            content,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        return document.RootElement.GetProperty("response").GetString();
    }

    /// <summary>
    /// Сгенерировать ответ с потоковой передачей
    /// </summary>
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string model,
        string prompt,
        string systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model,
            prompt,
            system = systemPrompt,
            stream = true,
            options = new { temperature = 0.7 }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/generate",
            content,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var document = JsonDocument.Parse(line);
            var responseText = document.RootElement.GetProperty("response").GetString();
            var done = document.RootElement.GetProperty("done").GetBoolean();

            if (!string.IsNullOrEmpty(responseText))
                yield return responseText;

            if (done) break;
        }
    }
}
