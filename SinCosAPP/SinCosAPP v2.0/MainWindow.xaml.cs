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
using Grpc.Net.Client;
using Grpc.Core;
using SignalGenerator.Server;
using System.Threading.Tasks;
using System.Threading;

namespace SinCosAPP_v2._0
{
    public partial class MainWindow : Window
    {
        private SignalGenerator.Server.SignalGenerator.SignalGeneratorClient _grpcClient;
        private AsyncDuplexStreamingCall<SignalRequest, SignalPoint> _stream;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public MainWindow()
        {
            
            // Инициализация компонентов WPF
            InitializeComponent();

            lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;

            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;

            txtEnglish.Text = SinCosAPP_v2._0.Resources.Resources.English;
            txtRussian.Text = SinCosAPP_v2._0.Resources.Resources.Russian;
            txtGerman.Text = SinCosAPP_v2._0.Resources.Resources.German;

            cmbLanguage.SelectedIndex = 0;


            DrawGraph();

            // ИНИЦИАЛИЗАЦИЯ GRPC И КНОПКИ 
            SetupGrpc();

            //  Логика кнопки "Применить" 
            SettingsControl.BtnApply.Click += async (s, e) => await StartStreaming();

            //  Логика кнопки "Очистить"
            SettingsControl.BtnClear.Click += (s, e) =>
            {
                // Останавливаем поток, чтобы новые точки не прилетали после очистки
                _cts.Cancel();
                _cts = new CancellationTokenSource();

                // Удаляем все точки
                WpfPlot1.Plot.Remove<ScottPlot.Plottables.Marker>();

                // Сбрасываем вид, чтобы увидеть пустой график
               
                WpfPlot1.Refresh();
            };

                // Подписываемся на кнопку "Применить" внутри UserControl
                SettingsControl.BtnApply.Click += async (s, e) => await StartStreaming();
        }
        private void SetupGrpc()
        {
            // Адрес должен совпадать с адресом запущенного сервера 
            var channel = GrpcChannel.ForAddress("https://localhost:7014");
            _grpcClient = new SignalGenerator.Server.SignalGenerator.SignalGeneratorClient(channel);
        }

        private async Task StartStreaming()
        {
            // Останавливаем старый поток
            _cts.Cancel();
            _cts.Dispose(); // Освобождаем ресурсы старого токена
            _cts = new CancellationTokenSource();

            try
            {
                //  Сначала открываем канал (стрим)
                _stream = _grpcClient.StreamSignal(cancellationToken: _cts.Token);

                //  Запускаем чтение в фоне
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var palette = new ScottPlot.Palettes.Category10();
                        var random = new Random();

                        // Используем ReadAllAsync с актуальным токеном
                        await foreach (var point in _stream.ResponseStream.ReadAllAsync(_cts.Token))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var marker = WpfPlot1.Plot.Add.Marker(point.X, point.Y);
                                marker.Color = palette.GetColor(random.Next(10));
                                marker.Size = 5;
                                WpfPlot1.Refresh();
                            });
                        }
                    }
                    catch (OperationCanceledException) { /* Игнорируем штатную отмену */ }
                    catch (Exception ex)
                    {
                        bool isCancelled = ex.Message.Contains("Cancel", StringComparison.OrdinalIgnoreCase);
                        if (!isCancelled)
                        {
                            Dispatcher.Invoke(() => MessageBox.Show($"Ошибка потока: {ex.Message}"));
                        }
                    }
                }, _cts.Token);

                //  ТОЛЬКО ТЕПЕРЬ отправляем настройки серверу, чтобы он начал слать точки
                await SendCurrentSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться: {ex.Message}");
            }
        } 

        private async Task SendCurrentSettings()
        {
            if (_stream == null) return;

            try
            {
                // Собираем данные из полей UserControl 
                var request = new SignalRequest
                {
                    MinX = double.TryParse(SettingsControl.TxtMinX.Text, out var minX) ? minX : 0,
                    MaxX = double.TryParse(SettingsControl.TxtMaxX.Text, out var maxX) ? maxX : 10,
                    MinY = double.TryParse(SettingsControl.TxtMinY.Text, out var minY) ? minY : -1,
                    MaxY = double.TryParse(SettingsControl.TxtMaxY.Text, out var maxY) ? maxY : 1,
                    Count = int.TryParse(SettingsControl.TxtCount.Text, out var count) ? count : 10
                };

                await _stream.RequestStream.WriteAsync(request);
            }
            catch (Exception) { /* Ошибка при закрытии приложения или стрима */ }
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

            WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.Culture = culture;

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