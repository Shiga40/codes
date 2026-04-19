using Microsoft.Win32;
using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPFLocalizeExtension.Engine;
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
        private CancellationTokenSource _cts;

        // Данные для графика
        private readonly List<double> _dataX = new();
        private readonly List<double> _dataY = new();
        private ScottPlot.Plottables.Scatter _scatter;

        public MainWindow()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            InitializeComponent();

            // Локализация
            lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;
            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;
            txtEnglish.Text = SinCosAPP_v2._0.Resources.Resources.English;
            txtRussian.Text = SinCosAPP_v2._0.Resources.Resources.Russian;
            txtGerman.Text = SinCosAPP_v2._0.Resources.Resources.German;
            cmbLanguage.SelectedIndex = 0;

            // Настройка ScottPlot
            DrawGraph();
            _scatter = WpfPlot1.Plot.Add.Scatter(_dataX, _dataY);
            _scatter.ConnectStyle = ConnectStyle.Straight;
            _scatter.Color = ScottPlot.Colors.Purple;
            _scatter.LineWidth = 2;

            // События
            SettingsControl.BtnApply.Click += async (s, e) => await StartStreaming();
            SettingsControl.BtnClear.Click += (s, e) => ClearGraph();
        }

        private void SetupGrpc(string address)
        {
            var channel = GrpcChannel.ForAddress(address);
            _grpcClient = new SignalGenerator.Server.SignalGenerator.SignalGeneratorClient(channel);
        }

        private async Task StartStreaming()
        {
            // 1. Валидация
            if (!double.TryParse(SettingsControl.TxtMinX.Text, out double minX) ||
                !double.TryParse(SettingsControl.TxtMaxX.Text, out double maxX) || maxX <= minX)
            {
                MessageBox.Show(SinCosAPP_v2._0.Resources.Resources.Error_InvalidX, "Валидация");
                return;
            }

            try
            {
                // 2. Инициализация потока, если он не запущен
                if (_stream == null || _cts == null || _cts.IsCancellationRequested)
                {
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();

                    string address = "http://localhost:5035";
                    SetupGrpc(address);

                    _stream = _grpcClient.StreamSignal(cancellationToken: _cts.Token);

                    
                    _ = Task.Run(async () => await ReceiveDataAsync(_cts.Token));
                }

                // 3. Отправка текущих настроек в поток
                await SendCurrentSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task ReceiveDataAsync(CancellationToken token)
        {
            try
            {
                if (_stream == null) return;

                await foreach (var point in _stream.ResponseStream.ReadAllAsync(token))
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Если новая точка X меньше последней — значит, пошел новый круг
                        if (_dataX.Count > 0 && point.X < _dataX.Last())
                        {
                            _dataX.Clear();
                            _dataY.Clear();
                        }

                        _dataX.Add(point.X);
                        _dataY.Add(point.Y);


                        WpfPlot1.Refresh();
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (RpcException ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show(GetFriendlyError(ex)));
                _stream = null;
            }
        }

        private async Task SendCurrentSettings()
        {
            if (_stream == null) return;

            try
            {
                var request = new SignalRequest
                {
                    MinX = double.TryParse(SettingsControl.TxtMinX.Text, out var minX) ? minX : 0,
                    MaxX = double.TryParse(SettingsControl.TxtMaxX.Text, out var maxX) ? maxX : 100,
                    MinY = double.TryParse(SettingsControl.TxtMinY.Text, out var minY) ? minY : -1,
                    MaxY = double.TryParse(SettingsControl.TxtMaxY.Text, out var maxY) ? maxY : 1,
                    Count = int.TryParse(SettingsControl.TxtCount.Text, out var count) ? count : 50
                };

                await _stream.RequestStream.WriteAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось обновить настройки: {ex.Message}");
            }
        }

        private void ClearGraph()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _stream = null;
            _dataX.Clear();
            _dataY.Clear();
            WpfPlot1.Plot.Axes.SetLimits(-10, 10, -1.5, 1.5);
            WpfPlot1.Refresh();
        }

        private void DrawGraph()
        {
            // Очищаем старые объекты ScottPlot
            WpfPlot1.Plot.Clear();

            // 1. Возвращаем статичные, бесконечные функции синуса и косинуса
            var funcSin = WpfPlot1.Plot.Add.Function(x => Math.Sin(x));
            var funcCos = WpfPlot1.Plot.Add.Function(x => Math.Cos(x));

            // Настраиваем названия для легенды (из ресурсов)
            funcSin.LegendText = SinCosAPP_v2._0.Resources.Resources.Sine;
            funcCos.LegendText = SinCosAPP_v2._0.Resources.Resources.Cosine;

            WpfPlot1.Refresh();

            // Настройка цвета и стиля
            funcSin.LineStyle.Color = ScottPlot.Colors.Red; 
            funcCos.LineStyle.Color = ScottPlot.Colors.Blue; 
            funcSin.LineWidth = 2;
            funcCos.LineWidth = 2;

            // Включаем легенду
            WpfPlot1.Plot.ShowLegend();

            // Устанавливаем начальный вид осей (диапазон по умолчанию)
            WpfPlot1.Plot.Axes.SetLimits(-10, 10, -1.5, 1.5);

            WpfPlot1.Refresh();
        }

        private void UpdateLegendLabels()
        {
            // Перебираем все объекты на графике
            // Получаем все объекты на графике
            foreach (var plottable in WpfPlot1.Plot.GetPlottables())
            {
                // проверяем наличие цвета линии. 
                // Это сработает для любого объекта, у которого есть LineStyle (функции, линии).
                if (plottable is var func && func.GetType().GetProperty("LineStyle") != null)
                {
                    // Используем 'dynamic' только для доступа к свойствам внутри условия, 
                    // чтобы компилятор не ругался на отсутствие типа
                    dynamic d = plottable;

                    try
                    {
                        if (d.LineStyle.Color == ScottPlot.Colors.Red)
                            d.LegendText = SinCosAPP_v2._0.Resources.Resources.Sine;
                        else if (d.LineStyle.Color == ScottPlot.Colors.Blue)
                            d.LegendText = SinCosAPP_v2._0.Resources.Resources.Cosine;
                    }
                    catch
                    {
                        // Пропускаем объекты, у которых нет нужных свойств (например, сетку)
                        continue;
                    }
                }
            }
        }
        private void UpdateLanguageAndMenu(string lang)
        {
            var culture = new CultureInfo(lang);
            LocalizeDictionary.Instance.Culture = culture;
            this.Title = SinCosAPP_v2._0.Resources.Resources.AppTitle;
            lblLanguage.Text = SinCosAPP_v2._0.Resources.Resources.LangLabel;
            txtEnglish.Text = SinCosAPP_v2._0.Resources.Resources.English;
            txtRussian.Text = SinCosAPP_v2._0.Resources.Resources.Russian;
            txtGerman.Text = SinCosAPP_v2._0.Resources.Resources.German;

            UpdateLegendLabels();

            ConfigureCustomMenu();
            WpfPlot1.Refresh();
        }

        private void ConfigureCustomMenu()
        {
            if (WpfPlot1.Menu is WpfPlotMenu safeCastedMenu)
            {
                safeCastedMenu.Clear();
                safeCastedMenu.Add(SinCosAPP_v2._0.Resources.Resources.MenuAutoscale, p => { p.Axes.AutoScale(); WpfPlot1.Refresh(); });
                safeCastedMenu.AddSeparator();
                safeCastedMenu.Add(SinCosAPP_v2._0.Resources.Resources.MenuSave, p =>
                {
                    var sfd = new SaveFileDialog { Filter = "PNG|*.png|JPEG|*.jpg", FileName = "Plot.png" };
                    if (sfd.ShowDialog() == true) p.Save(sfd.FileName, 1200, 800);
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
        private string GetFriendlyError(RpcException ex)
{
    if (ex.StatusCode == StatusCode.InvalidArgument)
    {
        return ex.Status.Detail switch
        {
            "ERR_INVALID_X_RANGE" => SinCosAPP_v2._0.Resources.Resources.Error_InvalidX, 
            _ => SinCosAPP_v2._0.Resources.Resources.Error_InvalidArgs // Общая ошибка параметров
        };
    }

    return ex.StatusCode switch
    {
        StatusCode.Unavailable => SinCosAPP_v2._0.Resources.Resources.Error_Unavailable,
        StatusCode.DeadlineExceeded => SinCosAPP_v2._0.Resources.Resources.Error_Deadline,
        _ => string.Format(SinCosAPP_v2._0.Resources.Resources.Error_RpcGeneric, ex.Status.Detail)
    };
}
    }
}