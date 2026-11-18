using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TemperatureSensor
{
    // Класс датчика с учетом окружающей среды
    public class EnvironmentAwareSensor
    {
        private readonly Random _random = new Random();
        private double _baseTemperature = 22.0;
        private readonly double _noiseAmplitude = 3.0;
        private bool _doorOpen = false;
        private bool _acOn = false;
        private bool _heaterOn = false;
        private int _peopleCount = 0;

        public SensorData ReadData()
        {
            // Базовые показания
            double noise = (_random.NextDouble() * 2 - 1) * _noiseAmplitude;
            double temperature = _baseTemperature + noise;
            double humidity = 60 + (temperature - _baseTemperature) * 2 + (_random.NextDouble() * 2 - 1) * 5;

            // Влияние внешних факторов
            if (_doorOpen) temperature += 3.0;
            if (_acOn) temperature -= 2.0;
            if (_heaterOn) temperature += 4.0;
            temperature += _peopleCount * 0.5;

            return new SensorData
            {
                Temperature = Math.Round(temperature, 2),
                Humidity = Math.Round(humidity, 1),
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["doorOpen"] = _doorOpen,
                    ["acOn"] = _acOn,
                    ["heaterOn"] = _heaterOn,
                    ["peopleCount"] = _peopleCount
                }
            };
        }

        public void OpenDoor() { _doorOpen = true; }
        public void CloseDoor() { _doorOpen = false; }
        public void TurnOnAC() { _acOn = true; }
        public void TurnOffAC() { _acOn = false; }
        public void TurnOnHeater() { _heaterOn = true; }
        public void TurnOffHeater() { _heaterOn = false; }
        public void AddPerson() { _peopleCount++; }
        public void RemovePerson() { if (_peopleCount > 0) _peopleCount--; }
    }

    public class SensorData
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    // График с временной шкалой
    public class GraphForm : Form
    {
        private readonly List<double> _data = new List<double>();
        private readonly List<DateTime> _times = new List<DateTime>();
        private readonly Label _lblTemp = new Label();
        private readonly Label _lblStatus = new Label();
        private readonly EnvironmentAwareSensor _sensor = new EnvironmentAwareSensor();
        private readonly Timer _timer = new Timer();
        private int _count = 0;

        public GraphForm()
        {
            Text = "IoT Датчик температуры - График";
            Size = new Size(700, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            // Информационная панель
            Panel info = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(240, 240, 240) };

            _lblTemp.Text = "Температура: --°C";
            _lblTemp.Location = new Point(20, 10);
            _lblTemp.AutoSize = true;
            _lblTemp.Font = new Font("Arial", 12, FontStyle.Bold);
            _lblTemp.ForeColor = Color.Red;

            _lblStatus.Text = "Статус: Ожидание...";
            _lblStatus.Location = new Point(20, 35);
            _lblStatus.AutoSize = true;
            _lblStatus.Font = new Font("Arial", 10);
            _lblStatus.ForeColor = Color.Blue;

            info.Controls.AddRange(new Control[] { _lblTemp, _lblStatus });
            Controls.Add(info);

            // Кнопки управления
            Panel btnPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };

            Button btnAdd = new Button { Text = "Добавить показание", Location = new Point(20, 5), Size = new Size(150, 30) };
            btnAdd.Click += (s, e) => AddReading();

            Button btnClear = new Button { Text = "Очистить", Location = new Point(180, 5), Size = new Size(100, 30) };
            btnClear.Click += (s, e) => { _data.Clear(); _times.Clear(); _count = 0; Invalidate(); };

            btnPanel.Controls.AddRange(new Control[] { btnAdd, btnClear });
            Controls.Add(btnPanel);

            this.Paint += GraphForm_Paint;

            // Таймер автоматического обновления
            _timer.Interval = 2000;
            _timer.Tick += (s, e) => AddReading();
            _timer.Start();

            // Добавляем начальные точки
            for (int i = 0; i < 5; i++)
                AddReading();

            // Запускаем имитацию внешних воздействий
            _ = SimulateEnvironmentalChanges();
        }

        private void AddReading()
        {
            var data = _sensor.ReadData();
            _data.Add(data.Temperature);
            _times.Add(data.Timestamp);
            _count++;

            _lblTemp.Text = $"Температура: {data.Temperature:F1}°C | Влажность: {data.Humidity}%";

            // Исправлено: явное преобразование object в нужные типы
            bool doorOpen = (bool)data.Metadata["doorOpen"];
            bool acOn = (bool)data.Metadata["acOn"];
            bool heaterOn = (bool)data.Metadata["heaterOn"];
            int peopleCount = (int)data.Metadata["peopleCount"];

            string status = $"Дверь: {(doorOpen ? "открыта" : "закрыта")} | ";
            status += $"Кондиционер: {(acOn ? "вкл" : "выкл")} | ";
            status += $"Обогреватель: {(heaterOn ? "вкл" : "выкл")} | ";
            status += $"Людей: {peopleCount}";
            _lblStatus.Text = status;

            // ЗАДАНИЕ 1: сохраняем строку с температурой и влажностью в текстовый файл
            string line = $"{data.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ};{data.Temperature:F1};{data.Humidity}";
            File.AppendAllText("sensor_state.txt", line + Environment.NewLine);

            // Вывод в консоль (IoT логирование)
            Console.WriteLine($"📊 [{data.Timestamp:HH:mm:ss}] Температура: {data.Temperature:F1}°C, Влажность: {data.Humidity}%");
            Console.WriteLine($"   Статус: {status}");
            Console.WriteLine(new string('-', 60));

            Invalidate();
        }

        private void GraphForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(80, 100, 550, 200);
            g.FillRectangle(new SolidBrush(Color.WhiteSmoke), rect);
            g.DrawRectangle(new Pen(Color.Black), rect);

            if (_data.Count == 0) return;

            double min = _data[0], max = _data[0];
            for (int i = 1; i < _data.Count; i++)
            {
                if (_data[i] < min) min = _data[i];
                if (_data[i] > max) max = _data[i];
            }
            min -= 1; max += 1;

            using (Font font = new Font("Arial", 9))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                // Ось Y
                for (int i = 0; i <= 5; i++)
                {
                    float y = rect.Bottom - rect.Height * i / 5f;
                    g.DrawLine(new Pen(Color.LightGray), rect.Left, y, rect.Right, y);
                    double val = min + (max - min) * i / 5;
                    g.DrawString($"{val:F1}°C", font, brush, 5, y - 7);
                }

                // Ось X
                for (int i = 0; i < _times.Count; i++)
                {
                    float x = rect.Left + i * rect.Width / (float)(_data.Count - 1);
                    if (i % 2 == 0 || i == _times.Count - 1)
                        g.DrawString(_times[i].ToString("HH:mm:ss"), font, brush, x - 15, rect.Bottom + 5);
                }
            }

            // График
            if (_data.Count > 1)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                using (Brush pointBrush = new SolidBrush(Color.Red))
                {
                    for (int i = 0; i < _data.Count; i++)
                    {
                        float x = rect.Left + i * rect.Width / (float)(_data.Count - 1);
                        float y = rect.Bottom - (float)((_data[i] - min) / (max - min) * rect.Height);

                        g.FillEllipse(pointBrush, x - 4, y - 4, 8, 8);
                        g.DrawEllipse(new Pen(Color.Black), x - 4, y - 4, 8, 8);

                        using (Font f = new Font("Arial", 8))
                        using (Brush b = new SolidBrush(Color.Black))
                            g.DrawString($"{_data[i]:F1}°C", f, b, x - 12, y - 20);

                        if (i > 0)
                        {
                            float prevX = rect.Left + (i - 1) * rect.Width / (float)(_data.Count - 1);
                            float prevY = rect.Bottom - (float)((_data[i - 1] - min) / (max - min) * rect.Height);
                            g.DrawLine(pen, prevX, prevY, x, y);
                        }
                    }
                }
            }

            using (Font f = new Font("Arial", 10, FontStyle.Bold))
            using (Brush b = new SolidBrush(Color.Black))
            {
                g.DrawString("Время", f, b, rect.Left + rect.Width / 2 - 20, rect.Bottom + 30);
                g.DrawString("Температура (°C)", f, b, rect.Left - 20, rect.Top - 30);
            }
        }

        // Имитация внешних воздействий (как в IoT)
        private async Task SimulateEnvironmentalChanges()
        {
            await Task.Delay(TimeSpan.FromSeconds(8));

            _sensor.OpenDoor();
            _lblStatus.Text += "  Дверь открыта!";

            await Task.Delay(TimeSpan.FromSeconds(12));

            _sensor.TurnOnAC();
            _sensor.CloseDoor();
            _lblStatus.Text += " ❄️ Кондиционер включен!";

            await Task.Delay(TimeSpan.FromSeconds(15));

            _sensor.TurnOnHeater();
            _sensor.TurnOffAC();
            _lblStatus.Text += "  Обогреватель включен!";

            await Task.Delay(TimeSpan.FromSeconds(12));

            _sensor.AddPerson();
            _sensor.AddPerson();
            _sensor.AddPerson();
            _lblStatus.Text += " 👥 Добавлено 3 человека!";

            await Task.Delay(TimeSpan.FromSeconds(10));

            _sensor.TurnOffHeater();
            _lblStatus.Text += " ️ Обогреватель выключен!";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            base.OnFormClosed(e);
        }
    }

    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }
}
