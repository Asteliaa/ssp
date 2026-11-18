using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🌡️ MQTT Client Test (v3.x)");
        Console.WriteLine("============================");

        try
        {
            // Проверяем наличие DLL
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MQTTnet.dll");
            if (File.Exists(dllPath))
                Console.WriteLine($"✅ DLL найдена: {dllPath}");
            else
                Console.WriteLine($"❌ DLL НЕ найдена: {dllPath}");

            // Создаем фабрику и клиент
            MqttFactory factory = new MqttFactory();
            MqttClient client = (MqttClient)factory.CreateMqttClient();
            Console.WriteLine("✅ Клиент создан");

            // Настройки подключения
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com", 1883)
                .WithClientId("TestClient_" + Guid.NewGuid().ToString().Substring(0, 6))
                .WithCleanSession()
                .Build();

            // ИСПРАВЛЕНО: правильные события для MQTTnet 3.x
            client.ConnectedHandler = new MqttClientConnectedHandlerDelegate((args) =>
            {
                Console.WriteLine($"✅ Подключено!");
                client.SubscribeAsync("test/topic", MqttQualityOfServiceLevel.AtLeastOnce);
                Console.WriteLine("📬 Подписались на test/topic");
            });

            client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate((args) =>
            {
                Console.WriteLine("❌ Отключено");
            });

            client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate((args) =>
            {
                string msg = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                Console.WriteLine($"📩 Получено: {msg}");
            });

            // Подключаемся
            await client.ConnectAsync(options);
            Console.WriteLine("🔗 Соединение установлено");

            // Отправляем сообщение
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("test/topic")
                .WithPayload($"Hello from MQTTnet 3.x! {DateTime.Now:HH:mm:ss}")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await client.PublishAsync(message);
            Console.WriteLine("📤 Сообщение отправлено");

            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();

            await client.DisconnectAsync();
            Console.WriteLine("🔌 Отключено");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }
    }
}
