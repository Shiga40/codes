using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;

namespace SineCosineAPP;

public partial class MainWindow : Window
{
    private const string ConfigPath = "lang.config";

    public MainWindow()
    {
        // 1. Загружаем сохраненный язык ПЕРЕД инициализацией интерфейса
        if (File.Exists(ConfigPath))
        {
            string cultureCode = File.ReadAllText(ConfigPath);
            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        InitializeComponent();

        // 2. Рисуем графики через ScottPlot
        DrawGraph();
    }

    private void DrawGraph()
{
    // Очищаем старые данные перед отрисовкой
    WpfPlot1.Plot.Clear();

    // Добавляем математическую функцию синуса
    // Она будет бесконечной и пересчитываться автоматически при каждом сдвиге
    var sinFunc = WpfPlot1.Plot.Add.Function(Math.Sin);
    sinFunc.LegendText = "Sin(x)";
    sinFunc.LineWidth = 2;
    

    // Добавляем математическую функцию косинуса
    var cosFunc = WpfPlot1.Plot.Add.Function(Math.Cos);
    cosFunc.LegendText = "Cos(x)";
    cosFunc.LineWidth = 2;
    

    // Устанавливаем начальные границы, чтобы не было пустоты
    WpfPlot1.Plot.Axes.SetLimitsX(-10, 10);
    WpfPlot1.Plot.Axes.SetLimitsY(-1.5, 1.5);

    // Включаем легенду и обновляем график
    WpfPlot1.Plot.ShowLegend();
    WpfPlot1.Refresh();
}

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbLanguage.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string lang = item.Tag.ToString();
            File.WriteAllText(ConfigPath, lang);

            MessageBox.Show("Please restart / Перезапустите программу");
        }
    }
}