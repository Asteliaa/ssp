using MQTTnet;
using MQTTnet.Client;

using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

using System.Text;

using System.Text.Json;
using System.Drawing;

namespace IoTDeviceSimulator
{
    // ============================================================
    // 1. КЛАССЫ ДАННЫХ
    // ============================================================

    public class SensorData
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Illuminance { get; set; }   // ЗАДАНИЕ: освещенность, лк
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // ============================================================
    // 2. СИМУЛЯТОР ДАТЧИКА
    // ============================================================

    public class EnvironmentAwareSensor
    {
        private readonly Random _random = new Random();
        private double _temperature = 22.0;
        private double _humidity = 60.0;
        private DateTime _lastUpdate = DateTime.UtcNow;

        // Внешние факторы
        private bool _isDoorOpen = false;
        private bool _isHeaterOn = false;
        private bool _isAirConditionerOn = false;
        private bool _isLightOn = false;   // ЗАДАНИЕ: освещение
        private int _peopleCount = 0;

        public SensorData ReadData()
        {
            var now = DateTime.UtcNow;
            double deltaMinutes = (now - _lastUpdate).TotalMinutes;
            _lastUpdate = now;

            // Ограничиваем дельту, чтобы избежать скачков при паузах
            if (deltaMinutes > 5) deltaMinutes = 0.1;

            // ДОП. ЗАДАНИЕ: ускорение модельного времени (в сценарии dop 1 с = 0,5 мин)
            deltaMinutes *= TimeScale;

            // 1. Влияние внешних факторов
            double tempChange = 0.0;
            double humidityChange = 0.0;

            // Влияние открытой двери
            if (_isDoorOpen)
            {
                double outsideTemp = 15.0 + (_random.NextDouble() * 2 - 1) * 3.0;
                tempChange += (outsideTemp - _temperature) * 0.02 * deltaMinutes;
                humidityChange += (40 - _humidity) * 0.01 * deltaMinutes;
            }

            // Влияние обогревателя
            if (_isHeaterOn)
            {
                tempChange += 2.0 * deltaMinutes;
                humidityChange -= 0.5 * deltaMinutes;
            }

            // Влияние кондиционера
            if (_isAirConditionerOn)
            {
                tempChange -= 1.5 * deltaMinutes;
                humidityChange += 0.3 * deltaMinutes;
            }

            // ЗАДАНИЕ: влияние освещения (лампы немного греют воздух)
            if (_isLightOn)
            {
                tempChange += 0.3 * deltaMinutes;
            }

            // Влияние людей
            tempChange += _peopleCount * 0.08 * deltaMinutes;
            humidityChange += _peopleCount * 0.05 * deltaMinutes;

            // 2. Естественный дрейф к комнатной температуре (если нет воздействий)
            if (!_isDoorOpen && !_isHeaterOn && !_isAirConditionerOn && !_isLightOn && _peopleCount == 0)
            {
                tempChange += (22.0 - _temperature) * 0.005 * deltaMinutes;
                humidityChange += (60.0 - _humidity) * 0.003 * deltaMinutes;
            }

            // 3. Суточные колебания (синусоида)
            double dailyCycle = 5.0 * Math.Sin(
                2 * Math.PI * (now.Hour + now.Minute / 60.0) / 24.0
            );
            tempChange += (dailyCycle - (_temperature - 22)) * 0.001 * deltaMinutes;

            // 4. Случайный шум
            double noise = (_random.NextDouble() * 2 - 1) * 0.3;
            tempChange += noise * deltaMinutes;
            humidityChange += (_random.NextDouble() * 2 - 1) * 0.2 * deltaMinutes;

            // 5. Применяем изменения
            _temperature += tempChange;
            _humidity += humidityChange;

            // 6. Ограничиваем значения
            _temperature = Math.Clamp(_temperature, -10, 45);
            _humidity = Math.Clamp(_humidity, 15, 95);

            return new SensorData
            {
                Temperature = Math.Round(_temperature, 2),
                Humidity = Math.Round(_humidity, 1),
                Illuminance = Math.Round((_isLightOn ? 500.0 : 50.0) + (_random.NextDouble() * 2 - 1) * 10),
                Timestamp = now,
                Metadata = new Dictionary<string, object>
                {
                    ["isDoorOpen"] = _isDoorOpen,
                    ["isHeaterOn"] = _isHeaterOn,
                    ["isAirConditionerOn"] = _isAirConditionerOn,
                    ["isLightOn"] = _isLightOn,
                    ["peopleCount"] = _peopleCount,
                    ["deltaMinutes"] = Math.Round(deltaMinutes, 2)
                }
            };
        }

        // Методы управления средой
        public void OpenDoor() { _isDoorOpen = true; Console.WriteLine("🚪 Дверь ОТКРЫТА"); }
        public void CloseDoor() { _isDoorOpen = false; Console.WriteLine("🚪 Дверь ЗАКРЫТА"); }
        public void TurnOnHeater() { _isHeaterOn = true; Console.WriteLine("🔥 Обогреватель ВКЛЮЧЕН"); }
        public void TurnOffHeater() { _isHeaterOn = false; Console.WriteLine("🔥 Обогреватель ВЫКЛЮЧЕН"); }
        public void TurnOnAC() { _isAirConditionerOn = true; Console.WriteLine("❄️ Кондиционер ВКЛЮЧЕН"); }
        public void TurnOffAC() { _isAirConditionerOn = false; Console.WriteLine("❄️ Кондиционер ВЫКЛЮЧЕН"); }
        public void TurnOnLight() { _isLightOn = true; Console.WriteLine("💡 Освещение ВКЛЮЧЕНО"); }
        public void TurnOffLight() { _isLightOn = false; Console.WriteLine("💡 Освещение ВЫКЛЮЧЕНО"); }
        public void AddPerson(bool log = true) { _peopleCount++; if (log) Console.WriteLine($"🧑 Добавлен человек. Всего: {_peopleCount}"); }
        public void RemovePerson() { if (_peopleCount > 0) _peopleCount--; Console.WriteLine($"🧑 Удален человек. Всего: {_peopleCount}"); }

        // ДОП. ЗАДАНИЕ: число людей и ручная установка температуры (для проверки сценария)
        public int PeopleCount => _peopleCount;
        public double TimeScale { get; set; } = 1.0;
        public void SetTemperature(double value) { _temperature = Math.Clamp(value, -10, 45); Console.WriteLine($"🌡️ Температура установлена: {_temperature:F1}°C"); }
    }

    // ============================================================
    // 3. ОСНОВНАЯ ПРОГРАММА
    // ============================================================

    class Program
    {
        // Настройки подключения к MQTT брокеру
        // Для тестирования можно использовать публичный брокер: test.mosquitto.org
        private const string MQTT_BROKER = "test.mosquitto.org";  // Публичный тестовый брокер
        private const int MQTT_PORT = 1883;  // 1883 - без TLS, 8883 - с TLS
        // Идентификатор и топик сделаны уникальными, чтобы не пересекаться с другими пользователями публичного брокера
        private static readonly string DEVICE_ID = "simulated_device_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private static readonly string MQTT_TOPIC = "iot/devices/" + DEVICE_ID + "/data";

        // ЗАДАНИЕ: порог температуры для гудка, файлы и число точек графика
        private const double TEMP_ALARM = 22.0;
        private const int POINTS_COUNT = 20;

        // ДОП. ЗАДАНИЕ: 15 человек, дверь закрыта, свет выключен, температура дошла до 10 °C -> 2 гудка
        private const int DOP_PEOPLE = 15;
        private const double DOP_TEMP = 10.0;
        private const double DOP_TIME_SCALE = 30.0;   // 1 с работы программы = 0,5 мин модели
        private static bool _dopAlarmDone = false;

        // Беззвучный режим (--silent): гудки не воспроизводятся, а только записываются в beep_log.txt
        private static bool _silent = false;

        // Компактный вывод: одна строка на показание, чтобы консоль не уплывала (включается ключом --dop или командой view)
        private static bool _compact = false;
        private static readonly string BEEP_FILE = Path.Combine(AppContext.BaseDirectory, "beep_log.txt");
        private static readonly string LOG_FILE = Path.Combine(AppContext.BaseDirectory, "sensor_log.txt");
        private static readonly string RECEIVED_FILE = Path.Combine(AppContext.BaseDirectory, "received_data.txt");
        private static readonly string POINTS_FILE = Path.Combine(AppContext.BaseDirectory, "points20.txt");
        private static readonly string CHART_FILE = Path.Combine(AppContext.BaseDirectory, "chart20.png");
        private static readonly List<(DateTime Time, double Temperature)> _points = new();
        private static readonly object _pointsLock = new object();

        // Для реального облака (например Azure) раскомментируйте:
        // private const string MQTT_BROKER = "your-iot-hub.azure-devices.net";
        // private const int MQTT_PORT = 8883;
        // private const string DEVICE_ID = "your-device-id";
        // private const string USERNAME = "your-iot-hub.azure-devices.net/your-device-id?api-version=2021-04-12";
        // private const string PASSWORD = "SharedAccessSignature sr=...";

        private static IMqttClient? _mqttClient;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,  // Для экономии трафика
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            _silent = args.Contains("--silent");
            _compact = args.Contains("--dop");
            if (_silent) Console.WriteLine("🔇 Беззвучный режим: гудки записываются в beep_log.txt");
            Console.WriteLine("🌡️ IoT Simulator v1.0");
            Console.WriteLine("========================================");
            Console.WriteLine("Управление средой (вводите команды):");
            Console.WriteLine("  door    - открыть/закрыть дверь");
            Console.WriteLine("  heater  - включить/выключить обогреватель");
            Console.WriteLine("  ac      - включить/выключить кондиционер");
            Console.WriteLine("  light   - включить/выключить освещение");
            Console.WriteLine("  add     - добавить человека");
            Console.WriteLine("  remove  - убрать человека");
            Console.WriteLine("  status  - показать текущее состояние");
            Console.WriteLine("  dop     - доп. задание: 15 человек, дверь закрыта, свет выключен (латиницей; можно доп)");
            Console.WriteLine("  temp    - задать температуру вручную");
            Console.WriteLine("  view    - компактный/подробный вывод (одна строка на показание)");
            Console.WriteLine("  exit    - выход");
            Console.WriteLine("========================================\n");

            // Создаем симулятор датчика
            var sensor = new EnvironmentAwareSensor();

            // Инициализируем MQTT клиент
            await InitializeMqttClient();

            // Запускаем фоновую задачу для симуляции случайных событий (опционально)
            // _ = SimulateRandomEvents(sensor);

            // Запускаем задачу чтения консольных команд
            _ = Task.Run(() => ProcessCommands(sensor));

            // ДОП. ЗАДАНИЕ: при запуске с ключом --dop сценарий стартует сам, без ввода команды
            if (args.Contains("--dop")) StartDopScenario(sensor);

            // Основной цикл сбора и отправки данных
            var lastStatsTime = DateTime.UtcNow;
            int messagesSent = 0;
            int errorsCount = 0;
            int cycleCount = 0;

            while (true)
            {
                try
                {
                    // 1. Читаем данные с датчика
                    var sensorData = sensor.ReadData();

                    // ЗАДАНИЕ: как только температура падает ниже 22 градусов, выдаем гудок
                    if (sensorData.Temperature < TEMP_ALARM)
                    {
                        if (!_compact) Console.WriteLine($"🔔 Температура {sensorData.Temperature:F1}°C ниже {TEMP_ALARM:F0}°C!");
                        Beep(1000, 300, "температура ниже 22", show: !_compact);
                    }

                    // ДОП. ЗАДАНИЕ: 15 человек, дверь закрыта, свет выключен, температура дошла до 10 °C -> 2 гудка
                    bool dopCondition = (int)sensorData.Metadata!["peopleCount"] == DOP_PEOPLE
                        && !(bool)sensorData.Metadata["isDoorOpen"]
                        && !(bool)sensorData.Metadata["isLightOn"]
                        && sensorData.Temperature <= DOP_TEMP;
                    if (dopCondition && !_dopAlarmDone)
                    {
                        Console.WriteLine($"🔔🔔 ДОП: {DOP_PEOPLE} человек, дверь закрыта, свет выключен, температура {sensorData.Temperature:F2}°C дошла до {DOP_TEMP:F0}°C!");
                        Beep(1500, 300, "доп. задание, гудок 1");
                        await Task.Delay(200);
                        Beep(1500, 300, "доп. задание, гудок 2");
                        _dopAlarmDone = true;   // 2 гудка один раз при наступлении условия
                        sensor.TimeScale = 1.0; // сценарий завершен: обычный ход времени
                    }
                    else if (!dopCondition)
                    {
                        _dopAlarmDone = false;  // условие нарушено: при повторном наступлении снова 2 гудка
                    }

                    // ЗАДАНИЕ: выводим получаемые данные в файл на диске
                    File.AppendAllText(LOG_FILE,
                        $"{sensorData.Timestamp:yyyy-MM-dd HH:mm:ss};T={sensorData.Temperature:F2};H={sensorData.Humidity:F1};L={sensorData.Illuminance:F0}" + Environment.NewLine);

                    // 2. Формируем телеметрию
                    var telemetry = new
                    {
                        deviceId = DEVICE_ID,
                        temperature = sensorData.Temperature,
                        humidity = sensorData.Humidity,
                        illuminance = sensorData.Illuminance,
                        timestamp = sensorData.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        metadata = sensorData.Metadata
                    };

                    string jsonPayload = JsonSerializer.Serialize(telemetry, _jsonOptions);

                    // 3. Отправляем в MQTT
                    if (_mqttClient != null && _mqttClient.IsConnected)
                    {
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(MQTT_TOPIC)
                            .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                            .WithRetainFlag(false)
                            .Build();

                        var publishResult = await _mqttClient.PublishAsync(message);

                        if (publishResult.ReasonCode == MqttClientPublishReasonCode.Success)
                        {
                            messagesSent++;

                            if (_compact)
                            {
                                // одна строка на показание; 🔔 – сработал гудок «ниже 22»
                                Console.WriteLine(string.Format("⏱ {0:HH:mm:ss} | 🌡 {1,5:F1}°C | 👥 {2,2} чел. | 🚪 {3} | 💡 {4} | ❄️ {5}{6}",
                                    DateTime.Now, sensorData.Temperature, sensorData.Metadata["peopleCount"],
                                    (bool)sensorData.Metadata["isDoorOpen"] ? "Открыта" : "Закрыта",
                                    (bool)sensorData.Metadata["isLightOn"] ? "Вкл " : "Выкл",
                                    (bool)sensorData.Metadata["isAirConditionerOn"] ? "Вкл" : "Выкл",
                                    sensorData.Temperature < TEMP_ALARM ? "  🔔" : ""));
                            }
                            else
                            {
                                // Выводим красивый график температуры
                                Console.Write($"📊 [{DateTime.Now:HH:mm:ss}] ");
                                DrawTemperatureChart(sensorData.Temperature);

                                // Выводим дополнительную информацию
                                Console.WriteLine(string.Format("   💧 Влажность: {0:F1}% | 👥 {1} чел. | 🚪 {2} | 🔥 {3} | ❄️ {4} | 💡 {5} ({6:F0} лк)",
                                    sensorData.Humidity,
                                    sensorData.Metadata["peopleCount"],
                                    (bool)sensorData.Metadata["isDoorOpen"] ? "Открыта" : "Закрыта",
                                    (bool)sensorData.Metadata["isHeaterOn"] ? "Вкл" : "Выкл",
                                    (bool)sensorData.Metadata["isAirConditionerOn"] ? "Вкл" : "Выкл",
                                    (bool)sensorData.Metadata["isLightOn"] ? "Вкл" : "Выкл",
                                    sensorData.Illuminance));
                            }
                        }
                        else
                        {
                            errorsCount++;
                            Console.WriteLine($"❌ Ошибка отправки: {publishResult.ReasonCode}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ MQTT не подключен. Попытка переподключения...");
                        await ReconnectMqttClient();
                    }

                    // 4. Статистика каждые 30 секунд
                    if ((DateTime.UtcNow - lastStatsTime).TotalSeconds >= 30)
                    {
                        Console.WriteLine($"\n📈 Статистика за 30 сек: Отправлено {messagesSent} сообщений, " +
                                         $"ошибок: {errorsCount}, циклов: {cycleCount}\n");
                        messagesSent = 0;
                        errorsCount = 0;
                        cycleCount = 0;
                        lastStatsTime = DateTime.UtcNow;
                    }

                    cycleCount++;

                    // 5. Пауза между измерениями
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
                    errorsCount++;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            } // <-- Закрывающая скобка для while
        } // <-- Закрывающая скобка для Main
          // ============================================================
          // 4. MQTT ФУНКЦИИ
          // ============================================================

        private static async Task InitializeMqttClient()
        {
            try
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                // Настройка опций подключения
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(MQTT_BROKER, MQTT_PORT)
                    .WithClientId(DEVICE_ID)
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));

                // Для защищенного подключения (раскомментировать если нужно)
                // if (MQTT_PORT == 8883)
                // {
                //     optionsBuilder.WithTlsOptions(new MqttClientTlsOptions
                //     {
                //         UseTls = true,
                //         IgnoreCertificateChainErrors = true,  // Только для тестирования!
                //         IgnoreCertificateRevocationErrors = true
                //     });
                // }

                // Для Azure IoT Hub (раскомментировать)
                // optionsBuilder.WithCredentials(USERNAME, PASSWORD);

                var options = optionsBuilder.Build();

                // Обработчики событий для MQTTnet v3.x
                _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate((e) =>
                {
                    Console.WriteLine($"✅ Подключено к MQTT брокеру: {MQTT_BROKER}");
                    // ЗАДАНИЕ: подписываемся на свой топик, чтобы получать данные обратно от сервера
                    _mqttClient!.SubscribeAsync(MQTT_TOPIC, MqttQualityOfServiceLevel.AtLeastOnce);
                    Console.WriteLine($"📬 Подписались на {MQTT_TOPIC}");
                });

                // ЗАДАНИЕ: обработка данных, возвращенных сервером
                _mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate((e) =>
                {
                    string msg = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    HandleReceived(msg);
                });

                _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate((e) =>
                {
                    Console.WriteLine($"⚠️ Отключено от MQTT. Причина: {e.Reason}");
                });

                // Подключаемся
                await _mqttClient.ConnectAsync(options);
            } // <-- Закрывающая скобка для try
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения к MQTT: {ex.Message}");
                Console.WriteLine("⚠️ Приложение продолжит работу в автономном режиме");
            }
        } // <-- Закрывающая скобка для метода
        private static async Task ReconnectMqttClient()
        {
            if (_mqttClient != null && !_mqttClient.IsConnected)
            {
                try
                {
                    await _mqttClient.ReconnectAsync();
                }
                catch
                {
                    // Игнорируем ошибки переподключения
                }
            }
        }

        // ============================================================
        // 5. ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        // ============================================================

        // Гудок: в обычном режиме Console.Beep, в беззвучном (--silent) только запись в файл
        private static void Beep(int frequency, int duration, string reason, bool show = true)
        {
            File.AppendAllText(BEEP_FILE,
                $"{DateTime.Now:HH:mm:ss.fff};{frequency} Гц;{duration} мс;{reason}" + Environment.NewLine);
            if (_silent)
            {
                if (show) Console.WriteLine($"   🔇 BEEP {frequency} Гц, {duration} мс ({reason})");
            }
            else
                Console.Beep(frequency, duration);
        }

        // ЗАДАНИЕ: запись полученных данных в файл и накопление 20 точек для графика
        private static void HandleReceived(string json)
        {
            try
            {
                File.AppendAllText(RECEIVED_FILE, json + Environment.NewLine);

                using var doc = JsonDocument.Parse(json);
                double temperature = doc.RootElement.GetProperty("temperature").GetDouble();
                DateTime time = DateTime.Parse(doc.RootElement.GetProperty("timestamp").GetString()!,
                    null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();

                lock (_pointsLock)
                {
                    if (_points.Count >= POINTS_COUNT) return;
                    _points.Add((time, temperature));
                    if (!_compact) Console.WriteLine($"📩 Получено от сервера, точка {_points.Count}/{POINTS_COUNT}: {temperature:F2}°C");

                    if (_points.Count == POINTS_COUNT)
                    {
                        var lines = _points.Select(p =>
                            $"{p.Time:HH:mm:ss};{p.Temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}");
                        File.WriteAllLines(POINTS_FILE, lines);
                        SaveChart(_points.Select(p => p.Temperature).ToList(), _points.Select(p => p.Time).ToList(), CHART_FILE);
                        Console.WriteLine($"💾 20 точек сохранены: {POINTS_FILE}");
                        Console.WriteLine($"📈 График сохранен: {CHART_FILE}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка обработки сообщения: {ex.Message}");
            }
        }

        // ЗАДАНИЕ: график температуры по 20 точкам (код отрисовки взят из GraphForm_Paint методички)
        private static void SaveChart(List<double> data, List<DateTime> times, string path)
        {
            using var bmp = new Bitmap(700, 360);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Rectangle rect = new Rectangle(80, 60, 550, 220);
            g.FillRectangle(new SolidBrush(Color.WhiteSmoke), rect);
            g.DrawRectangle(new Pen(Color.Black), rect);

            double min = data[0], max = data[0];
            for (int i = 1; i < data.Count; i++)
            {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }
            min -= 1; max += 1;

            using (Font font = new Font("Arial", 8))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                // Ось Y
                for (int i = 0; i <= 5; i++)
                {
                    float y = rect.Bottom - rect.Height * i / 5f;
                    g.DrawLine(new Pen(Color.LightGray), rect.Left, y, rect.Right, y);
                    double val = min + (max - min) * i / 5;
                    g.DrawString($"{val:F1}°C", font, brush, 20, y - 7);
                }

                // Ось X
                for (int i = 0; i < times.Count; i++)
                {
                    float x = rect.Left + i * rect.Width / (float)(data.Count - 1);
                    if (i % 2 == 0)
                        g.DrawString(times[i].ToString("HH:mm:ss"), font, brush, x - 18, rect.Bottom + 5);
                }
            }

            // График
            using (Pen pen = new Pen(Color.Red, 2))
            using (Brush pointBrush = new SolidBrush(Color.Red))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    float x = rect.Left + i * rect.Width / (float)(data.Count - 1);
                    float y = rect.Bottom - (float)((data[i] - min) / (max - min) * rect.Height);

                    g.FillEllipse(pointBrush, x - 4, y - 4, 8, 8);
                    g.DrawEllipse(new Pen(Color.Black), x - 4, y - 4, 8, 8);

                    if (i % 2 == 0)
                        using (Font f = new Font("Arial", 7))
                        using (Brush b = new SolidBrush(Color.Black))
                            g.DrawString($"{data[i]:F1}", f, b, x - 10, y - 20);

                    if (i > 0)
                    {
                        float prevX = rect.Left + (i - 1) * rect.Width / (float)(data.Count - 1);
                        float prevY = rect.Bottom - (float)((data[i - 1] - min) / (max - min) * rect.Height);
                        g.DrawLine(pen, prevX, prevY, x, y);
                    }
                }
            }

            using (Font f = new Font("Arial", 10, FontStyle.Bold))
            using (Brush b = new SolidBrush(Color.Black))
            {
                g.DrawString("Время", f, b, rect.Left + rect.Width / 2 - 20, rect.Bottom + 30);
                g.DrawString("Температура (°C)", f, b, rect.Left - 20, rect.Top - 30);
            }

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }

        private static void DrawTemperatureChart(double temperature, double min = -10, double max = 40)
        {
            int width = 40;
            int position = (int)((temperature - min) / (max - min) * width);
            position = Math.Clamp(position, 0, width);

            Console.Write("[");
            Console.Write(new string('░', position));
            Console.ForegroundColor = temperature switch
            {
                > 30 => ConsoleColor.Red,
                > 25 => ConsoleColor.Yellow,
                > 15 => ConsoleColor.Green,
                > 5 => ConsoleColor.Cyan,
                _ => ConsoleColor.Blue
            };
            Console.Write("█");
            Console.ResetColor();
            Console.Write(new string('░', width - position));
            Console.WriteLine($"] {temperature:F1}°C");
        }

        // ДОП. ЗАДАНИЕ: подготовка сценария (команда dop или запуск с ключом --dop)
        private static void StartDopScenario(EnvironmentAwareSensor sensor)
        {
            sensor.CloseDoor();
            sensor.TurnOffLight();
            sensor.TurnOffHeater();
            sensor.TurnOnAC();   // без охлаждения 15 человек только нагревают помещение
            while (sensor.PeopleCount > DOP_PEOPLE) sensor.RemovePerson();
            // при реальном ходе времени охлаждение до 10 °C заняло бы десятки минут
            sensor.TimeScale = DOP_TIME_SCALE;
            Console.WriteLine($"✅ Сценарий доп. задания: дверь закрыта, свет выключен, кондиционер включен, время ускорено в {DOP_TIME_SCALE:F0} раз. Люди заходят по одному каждые 2 с");
            // люди набираются постепенно: до 15 человек примерно за 30 секунд
            _ = Task.Run(async () =>
            {
                while (sensor.PeopleCount < DOP_PEOPLE)
                {
                    sensor.AddPerson(log: !_compact);   // в компактном режиме число людей видно в строке показания
                    if (sensor.PeopleCount < DOP_PEOPLE) await Task.Delay(TimeSpan.FromSeconds(2));
                }
                Console.WriteLine($"👥 Набралось {DOP_PEOPLE} человек. Ждем {DOP_TEMP:F0}°C");
            });
        }

        private static async Task ProcessCommands(EnvironmentAwareSensor sensor)
        {
            while (true)
            {
                try
                {
                    var input = Console.ReadLine()?.ToLower().Trim();

                    if (string.IsNullOrEmpty(input)) continue;

                    switch (input)
                    {
                        case "door":
                            Console.Write("Открыть или закрыть? (open/close): ");
                            var doorAction = Console.ReadLine()?.ToLower().Trim();
                            if (doorAction == "open") sensor.OpenDoor();
                            else if (doorAction == "close") sensor.CloseDoor();
                            break;

                        case "heater":
                            Console.Write("Включить или выключить? (on/off): ");
                            var heaterAction = Console.ReadLine()?.ToLower().Trim();
                            if (heaterAction == "on") sensor.TurnOnHeater();
                            else if (heaterAction == "off") sensor.TurnOffHeater();
                            break;

                        case "ac":
                            Console.Write("Включить или выключить? (on/off): ");
                            var acAction = Console.ReadLine()?.ToLower().Trim();
                            if (acAction == "on") sensor.TurnOnAC();
                            else if (acAction == "off") sensor.TurnOffAC();
                            break;

                        case "light":
                            Console.Write("Включить или выключить? (on/off): ");
                            var lightAction = Console.ReadLine()?.ToLower().Trim();
                            if (lightAction == "on") sensor.TurnOnLight();
                            else if (lightAction == "off") sensor.TurnOffLight();
                            break;

                        case "add":
                            sensor.AddPerson();
                            break;

                        // ДОП. ЗАДАНИЕ: подготовка сценария
                        case "dop":
                        case "доп":
                        case "вщз":   // dop, набранное в русской раскладке
                            StartDopScenario(sensor);
                            break;

                        case "view":
                        case "мшуц":   // view, набранное в русской раскладке
                            _compact = !_compact;
                            Console.WriteLine(_compact ? "📋 Компактный вывод: одна строка на показание" : "📋 Подробный вывод");
                            break;

                        case "temp":
                            Console.Write("Введите температуру, °C: ");
                            var tInput = Console.ReadLine()?.Trim().Replace(',', '.');
                            if (double.TryParse(tInput, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double tValue))
                                sensor.SetTemperature(tValue);
                            else
                                Console.WriteLine("Неверное число");
                            break;

                        case "remove":
                            sensor.RemovePerson();
                            break;

                        case "status":
                            var data = sensor.ReadData();
                            Console.WriteLine($"\n📊 Текущее состояние:");
                            Console.WriteLine($"   🌡️ Температура: {data.Temperature:F1}°C");
                            Console.WriteLine($"   💧 Влажность: {data.Humidity:F1}%");
                            bool isDoorOpen = (bool)data.Metadata["isDoorOpen"];
                            bool isHeaterOn = (bool)data.Metadata["isHeaterOn"];
                            bool isACOn = (bool)data.Metadata["isAirConditionerOn"];

                            Console.WriteLine($"   🚪 Дверь: {(isDoorOpen ? "Открыта" : "Закрыта")}");
                            Console.WriteLine($"   🔥 Обогреватель: {(isHeaterOn ? "Вкл" : "Выкл")}");
                            Console.WriteLine($"   ❄️ Кондиционер: {(isACOn ? "Вкл" : "Выкл")}");
                            Console.WriteLine($"   💡 Освещение: {((bool)data.Metadata["isLightOn"] ? "Вкл" : "Выкл")} ({data.Illuminance:F0} лк)");
                            Console.WriteLine($"   👥 Людей: {data.Metadata["peopleCount"]}\n");
                            break;

                        case "exit":
                            Console.WriteLine("Выход...");
                            Environment.Exit(0);
                            break;

                        default:
                            Console.WriteLine("Неизвестная команда. Доступные: door, heater, ac, light, add, remove, dop, temp, view, status, exit");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обработчике команд: {ex.Message}");
                }
            }
        }

        // Опциональная функция для автоматической симуляции событий
        private static async Task SimulateRandomEvents(EnvironmentAwareSensor sensor)
        {
            var random = new Random();
            await Task.Delay(TimeSpan.FromSeconds(10));

            while (true)
            {
                try
                {
                    int eventType = random.Next(0, 5);

                    switch (eventType)
                    {
                        case 0:
                            if (random.Next(2) == 0) sensor.OpenDoor();
                            else sensor.CloseDoor();
                            break;
                        case 1:
                            if (random.Next(2) == 0) sensor.TurnOnHeater();
                            else sensor.TurnOffHeater();
                            break;
                        case 2:
                            if (random.Next(2) == 0) sensor.TurnOnAC();
                            else sensor.TurnOffAC();
                            break;
                        case 3:
                            if (random.Next(2) == 0) sensor.AddPerson();
                            else sensor.RemovePerson();
                            break;
                        case 4:
                            // Ничего не делаем
                            break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(random.Next(10, 30)));
                }
                catch
                {
                    // Игнорируем ошибки
                }
            }
        }
    }
}
