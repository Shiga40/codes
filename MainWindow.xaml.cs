using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SineCosinePlot
{
    public partial class MainWindow : Window
    // пикселей на 1 радиан
    {
        private const double ScaleX = 40;     
        private const double ScaleY = 50;     
        private double zoom = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DrawGraph();
        }

        // определяем высоту и ширину
        private void DrawGraph()
        {
            PlotCanvas.Children.Clear();

            double width = PlotCanvas.ActualWidth;
            double height = PlotCanvas.ActualHeight;
            if (width < 100 || height < 100) return;

            double centerX = width / 2;
            double centerY = height / 2;

            // Оси
            DrawAxis(centerX, centerY, width, height);

            // === sin(x) ===
            var sinLine = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2.5
            };

            // === cos(x) ===
            var cosLine = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2.5
            };

            // Сколько точек 
            int pointsCount = 1500;
            double step = 8.0 * Math.PI / pointsCount;

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = -4 * Math.PI + i * step;
                double ySin = Math.Sin(x);
                double yCos = Math.Cos(x);

                double px = centerX + x * ScaleX * zoom;
                double pySin = centerY - ySin * ScaleY * zoom;
                double pyCos = centerY - yCos * ScaleY * zoom;

                sinLine.Points.Add(new Point(px, pySin));
                cosLine.Points.Add(new Point(px, pyCos));
            }

            PlotCanvas.Children.Add(sinLine);
            PlotCanvas.Children.Add(cosLine);

            // Подписи
            AddLabel("sin(x)", Brushes.Red, width  -80, 20);
            AddLabel("cos(x)", Brushes.Blue, width -80, 45);
        }

        private void DrawAxis(double cx, double cy, double w, double h)
        {

            // вертикальная ось
            var va = new Line { X1 = cx, Y1 = 0, X2 = cx, Y2 = h, Stroke = Brushes.Black, StrokeThickness = 1 };
            PlotCanvas.Children.Add(va);

            // горизонтальная ось
            var ha = new Line { X1 = 0, Y1 = cy, X2 = w, Y2 = cy, Stroke = Brushes.Black, StrokeThickness = 1 };
            PlotCanvas.Children.Add(ha);

            const double labelFontSize = 11;
            var labelBrush = Brushes.Black;
            // маленькие деления
            for (double x = -15; x <= 15; x += 0.5)
            {

                double px = cx + x * ScaleX * zoom;
                if (px < 0 || px > w) continue;
                var tick = new Line
                {
                    X1 = px,
                    Y1 = cy - 4,
                    X2 = px,
                    Y2 = cy + 4,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                PlotCanvas.Children.Add(tick);

                if (Math.Abs(x % 1.0) < 0.001) // целое число
                {
                    var label = new TextBlock
                    {
                        Text = x.ToString("0"),
                        FontSize = labelFontSize,
                        Foreground = labelBrush,
                        TextAlignment = TextAlignment.Center
                    };

                    // Текст под чёрточкой
                    Canvas.SetLeft(label, px - 12);     
                    Canvas.SetTop(label, cy + 8);       
                    PlotCanvas.Children.Add(label);
                }
            }

            for (double y = -10; y <= 10; y += 0.5)
            {

                double py = cy - y * ScaleY * zoom;
                if (py < 0 || py > h) continue;
                var tick = new Line
                {
                    X1 = cx - 4,      
                    Y1 = py,
                    X2 = cx + 4,      
                    Y2 = py,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                PlotCanvas.Children.Add(tick);
                if (Math.Abs(y % 1.0) < 0.001)
                {
                    var label = new TextBlock
                    {
                        Text = y.ToString("0"),
                        FontSize = labelFontSize,
                        Foreground = labelBrush,
                        TextAlignment = TextAlignment.Right
                    };

                    
                    Canvas.SetLeft(label, cx - 32);     
                    Canvas.SetTop(label, py - 9);       
                    PlotCanvas.Children.Add(label);
                }
            }
        }

        // подписи
        private void AddLabel(string text, Brush color, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            PlotCanvas.Children.Add(tb);
        }

        // zoom
        private void PlotCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) zoom *= 1.15;
            else zoom /= 1.15;

            zoom = Math.Max(0.4, Math.Min(8.0, zoom));
            DrawGraph();
        }

        // расширение окна 
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (IsLoaded) DrawGraph();
        }
    }
}