using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.WPF;
using WPFLocalizeExtension.Extensions;

namespace SinCosAPP_v2._0
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cmbLanguage.SelectedIndex = 0;
            DrawGraph();
        }

        private void DrawGraph()
        {
            WpfPlot1.Plot.Clear();

            // Создаем бесконечные математические функции
            var funcSin = WpfPlot1.Plot.Add.Function(x => Math.Sin(x));
            var funcCos = WpfPlot1.Plot.Add.Function(x => Math.Cos(x));

            // Настраиваем названия для легенды
            funcSin.LegendText = "Sin";
            funcCos.LegendText = "Cos";

            // цвет
            funcSin.LineStyle.Color = ScottPlot.Colors.Red;
            funcCos.LineStyle.Color = ScottPlot.Colors.Blue;


            // Настраиваем толщину
            funcSin.LineWidth = 2;
            funcCos.LineWidth = 2;

            // Включаем легенду
            WpfPlot1.Plot.ShowLegend();

            // Устанавливаем начальный вид 
            WpfPlot1.Plot.Axes.SetLimits(-10, 10, -1.5, 1.5);

            WpfPlot1.Refresh();
        }

        //создание нового контекстного меню
        private void UpdateLanguageAndMenu(string lang)
        {
            var culture = new System.Globalization.CultureInfo(lang);
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;
            if (lblLanguage != null)
                lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;

            // Перевод легенды
            var functions = WpfPlot1.Plot.GetPlottables<ScottPlot.Plottables.FunctionPlot>().ToList();
            if (functions.Count >= 2)
            {
                functions[0].LegendText = lang == "ru" ? "Синус" : "Sine";
                functions[1].LegendText = lang == "ru" ? "Косинус" : "Cosine";
            }

            // Настройка меню ScottPlot 5
            ConfigureCustomMenu(lang);

            WpfPlot1.Refresh();
        }

        private void ConfigureCustomMenu(string lang)
        {
            bool isRu = lang == "ru" || lang.StartsWith("ru-");

            var menu = WpfPlot1.Menu;
            menu.Clear(); // Удаляем стандартные пункты

            menu.Add(isRu ? "Автомасштабировать" : "Autoscale", p =>
            {
                p.Axes.AutoScale();
                WpfPlot1.Refresh();
            });

            menu.AddSeparator();

            menu.Add(isRu ? "Сохранить изображение как..." : "Save image as...", p =>
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "PNG|*.png|JPEG|*.jpg|Все файлы|*.*",
                    DefaultExt = "png",
                    FileName = "График_СинусКосинус.png"
                };

                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        p.Save(sfd.FileName, 1200, 800);
                        MessageBox.Show(isRu ? "График сохранён" : "Plot saved", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, isRu ? "Ошибка" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

           
        }


        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLanguage.SelectedItem is ComboBoxItem selectedItem)
            {
                string lang = selectedItem.Tag?.ToString() ?? "en";
                UpdateLanguageAndMenu(lang);

                
            }
        }
    }
}