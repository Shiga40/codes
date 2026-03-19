using Microsoft.Win32;
using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;

namespace SinCosAPP_v2._0
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            // 2. Инициализация компонентов WPF
            InitializeComponent();

            lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;

            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;

            txtEnglish.Text = SinCosAPP_v2._0.Resources.Resources.English;
            txtRussian.Text = SinCosAPP_v2._0.Resources.Resources.Russian;
            txtGerman.Text = SinCosAPP_v2._0.Resources.Resources.German;

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
            funcSin.LegendText = SinCosAPP_v2._0.Resources.Resources.Sine;
            funcCos.LegendText = SinCosAPP_v2._0.Resources.Resources.Cosine;

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
            var culture = new CultureInfo(lang);
            LocalizeDictionary.Instance.Culture = culture;

            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;

            lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;

            txtEnglish.Text = SinCosAPP_v2._0.Resources.Resources.English;
            txtRussian.Text = SinCosAPP_v2._0.Resources.Resources.Russian;
            txtGerman.Text = SinCosAPP_v2._0.Resources.Resources.German;

           
            var functions = WpfPlot1.Plot.GetPlottables<ScottPlot.Plottables.FunctionPlot>().ToList();
            if (functions.Count >= 2)
            {
                functions[0].LegendText = SinCosAPP_v2._0.Resources.Resources.Sine;
                functions[1].LegendText = SinCosAPP_v2._0.Resources.Resources.Cosine;
            }

            ConfigureCustomMenu();
            WpfPlot1.Refresh();
        }

        private void ConfigureCustomMenu()
        {
            
            if (WpfPlot1.Menu is WpfPlotMenu safeCastedMenu)
            {
                safeCastedMenu.Clear();

                // Используем ресурсы для названий пунктов меню
                safeCastedMenu.Add(SinCosAPP_v2._0.Resources.Resources.MenuAutoscale, p =>
                {
                    p.Axes.AutoScale();
                    WpfPlot1.Refresh();
                });

                safeCastedMenu.AddSeparator();

                safeCastedMenu.Add(SinCosAPP_v2._0.Resources.Resources.MenuSave, p =>
                {
                    var sfd = new SaveFileDialog
                    {
                        Filter = "PNG|*.png|JPEG|*.jpg",
                        FileName = "Plot.png"
                    };

                    if (sfd.ShowDialog() == true)
                    {
                        try
                        {
                            p.Save(sfd.FileName, 1200, 800);
                            
                            
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error");
                        }
                    }
                });
            }
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