using Grpc.Core;
using SignalGenerator.Server; // Этот namespace должен совпадать с тем, что в .proto

namespace SignalGenerator.Server.Services;

public class SignalGeneratorService : SignalGenerator.SignalGeneratorBase
{
    private readonly ILogger<SignalGeneratorService> _logger;

    public SignalGeneratorService(ILogger<SignalGeneratorService> logger)
    {
        _logger = logger;
    }

    public override async Task StreamSignal(
        IAsyncStreamReader<SignalRequest> requestStream,
        IServerStreamWriter<SignalPoint> responseStream,
        ServerCallContext context)
    {
        var rng = new Random();

        try
        {
            // Ждем первый и последующие запросы настроек от WPF
            await foreach (var request in requestStream.ReadAllAsync())
            {
                _logger.LogInformation($"Генерация: {request.Count} точек в диапазоне X[{request.MinX}:{request.MaxX}]");

                for (int i = 0; i < request.Count; i++)
                {
                    // Проверяем, не отключился ли клиент, чтобы не работать вхолостую
                    if (context.CancellationToken.IsCancellationRequested) break;

                    var point = new SignalPoint
                    {
                        X = request.MinX + (request.MaxX - request.MinX) * (i / (double)request.Count),
                        Y = request.MinY + (request.MaxY - request.MinY) * rng.NextDouble()
                    };

                    await responseStream.WriteAsync(point);

                    // Небольшая пауза, чтобы график "оживал" постепенно
                    await Task.Delay(5, context.CancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Стрим был отменен клиентом.");
        }
    }
}