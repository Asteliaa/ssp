using OllamaClient; using OllamaClient.Agents;
class CtxTest { static async Task Main(){
 var c=new OllamaClient.OllamaApiClient();
 var a=new CodeAssistantAgent(c,new AgentConfig{Model="llama3.2:1b",SystemPrompt="Вы — эксперт-разработчик на C#. Отвечайте кратко."});
 Console.WriteLine("Q1: Запомни число 42 и ответь одним словом: ОК");
 Console.WriteLine(await a.ProcessAsync("Запомни число 42 и ответь одним словом: ОК"));
 Console.WriteLine("Q2: Какое число я просил запомнить?");
 Console.WriteLine(await a.ProcessAsync("Какое число я просил запомнить?"));
}}
