using System;
using System.Collections.Generic;
using System.Drawing; // Простір імен для малювання (System.Drawing)
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GraphPlotterApp
{
    /// <summary>
    /// Клас, що відповідає за математичні обчислення.
    /// Дотримання принципу інкапсуляції: деталі формули сховані тут.
    /// </summary>
    public class FunctionCalculator
    {
        // Приватні поля (Encapsulation + Code Convention: _camelCase)
        private readonly double _startX;
        private readonly double _endX;
        private readonly double _step;

        public FunctionCalculator(double startX, double endX, double step)
        {
            _startX = startX;
            _endX = endX;
            _step = step;
        }

        // Приватний метод для обчислення Y за формулою
        // y = tg(0.5x) / (x^3 + 7.5)
        private double CalculateY(double x)
        {
            return Math.Tan(0.5 * x) / (Math.Pow(x, 3) + 7.5);
        }

        // Публічний метод для отримання готових точок
        public Dictionary<double, double> GetPoints()
        {
            var points = new Dictionary<double, double>();

            // Використовуємо decimal для циклу, щоб уникнути похибок округлення float/double при додаванні 0.1
            for (decimal x = (decimal)_startX; x <= (decimal)_endX; x += (decimal)_step)
            {
                double valX = (double)x;
                points[valX] = CalculateY(valX);
            }

            return points;
        }
    }

    /// <summary>
    /// Головна форма програми.
    /// Відповідає лише за відображення (UI).
    /// </summary>
    public class GraphForm : Form
    {
        // Поле для зберігання даних
        private Dictionary<double, double> _dataPoints;

        public GraphForm()
        {
            // Налаштування вікна
            this.Text = "Графік функції: y = tg(0.5x) / (x^3 + 7.5)";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(400, 300);

            // Важливі налаштування для перерисовки:
            // ResizeRedraw = true змушує форму викликати OnPaint при зміні розміру
            this.ResizeRedraw = true;
            // DoubleBuffered прибирає мерехтіння графіку при розтягуванні вікна
            this.DoubleBuffered = true;

            LoadData();
        }

        private void LoadData()
        {
            // Створення екземпляру калькулятора (ізоляція логіки)
            // Діапазон: [0.1; 1.2], крок 0.1
            var calculator = new FunctionCalculator(0.1, 1.2, 0.1);
            _dataPoints = calculator.GetPoints();
        }

        // Перевизначення методу малювання
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Отримуємо об'єкт Graphics для малювання
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Згладжування ліній

            // 1. Отримуємо розміри області малювання
            float width = this.ClientSize.Width;
            float height = this.ClientSize.Height;
            float padding = 50f; // Відступи від країв вікна

            // Перевірка, чи є дані
            if (_dataPoints == null || _dataPoints.Count == 0) return;

            // 2. Знаходимо мінімуми та максимуми для масштабування
            double minX = _dataPoints.Keys.Min();
            double maxX = _dataPoints.Keys.Max();
            double minY = _dataPoints.Values.Min();
            double maxY = _dataPoints.Values.Max();

            // Захист від ділення на нуль, якщо всі значення однакові
            if (Math.Abs(maxX - minX) < 0.0001) maxX += 1;
            if (Math.Abs(maxY - minY) < 0.0001) maxY += 1;

            // 3. Обчислюємо коефіцієнти масштабування
            // Ширина і висота області самого графіка (всередині відступів)
            float plotWidth = width - 2 * padding;
            float plotHeight = height - 2 * padding;

            float scaleX = plotWidth / (float)(maxX - minX);
            float scaleY = plotHeight / (float)(maxY - minY);

            // 4. Створюємо масив точок для екрану (Screen Coordinates)
            List<PointF> screenPoints = new List<PointF>();

            foreach (var point in _dataPoints)
            {
                // Формула переведення математичних координат у екранні:
                // X_screen = padding + (x - minX) * scaleX
                float pixelX = padding + (float)(point.Key - minX) * scaleX;

                // Y_screen = (height - padding) - (y - minY) * scaleY
                // (Увага: на екрані вісь Y йде зверху вниз, тому віднімаємо від нижнього краю)
                float pixelY = (height - padding) - (float)(point.Value - minY) * scaleY;

                screenPoints.Add(new PointF(pixelX, pixelY));
            }

            // 5. Малюємо елементи
            DrawAxes(g, padding, width, height);
            DrawGraph(g, screenPoints);
            DrawLabels(g, screenPoints, padding);
        }

        private void DrawAxes(Graphics g, float padding, float w, float h)
        {
            using (Pen axisPen = new Pen(Color.Gray, 1))
            {
                // Рамка навколо графіка
                g.DrawRectangle(axisPen, padding, padding, w - 2 * padding, h - 2 * padding);
            }
        }

        private void DrawGraph(Graphics g, List<PointF> points)
        {
            if (points.Count < 2) return;

            // Малюємо лінію графіка
            using (Pen graphPen = new Pen(Color.Blue, 2.5f))
            {
                g.DrawLines(graphPen, points.ToArray());
            }

            // Малюємо точки (вузли)
            using (Brush pointBrush = new SolidBrush(Color.Red))
            {
                float pointRadius = 3f;
                foreach (var p in points)
                {
                    g.FillEllipse(pointBrush, p.X - pointRadius, p.Y - pointRadius, pointRadius * 2, pointRadius * 2);
                }
            }
        }

        private void DrawLabels(Graphics g, List<PointF> points, float padding)
        {
            // Виводимо підписи значень біля точок (опціонально, для наочності)
            using (Font font = new Font("Arial", 8))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                int index = 0;
                foreach (var item in _dataPoints)
                {
                    PointF loc = points[index];
                    string text = $"x:{item.Key:0.0}";

                    // Малюємо текст трохи вище точки
                    g.DrawString(text, font, textBrush, loc.X - 10, loc.Y - 20);
                    index++;
                }
            }
        }
    }

    // Точка входу в програму
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Запуск нашої форми
            Application.Run(new GraphForm());
        }
    }
}