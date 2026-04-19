using Grpc.Core;
using SignalGenerator.Server; 
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalGenerator.Server.Services
{
    public class SignalService : SignalGenerator.SignalGeneratorBase
    {
        public override async Task StreamSignal(
       IAsyncStreamReader<SignalRequest> requestStream,
       IServerStreamWriter<SignalPoint> responseStream,
       ServerCallContext context)
        {
            // валидация X
            if (!await requestStream.MoveNext()) return;
            var settings = requestStream.Current;

            if (settings.Count <= 0 || settings.MaxX <= settings.MinX)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ERR_INVALID_X_RANGE"));
            }

            // Фоновая задача для обновления настроек "на лету"
            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (await requestStream.MoveNext(context.CancellationToken))
                    {
                        settings = requestStream.Current; // Обновляем параметры без остановки стрима
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Бесконечная генерация данных (Xn < Xn+1)
            double currentX = settings.MinX;
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    double step = (settings.MaxX - settings.MinX) / settings.Count;

                   
                    // Находим центр (смещение по вертикали) и радиус (амплитуду)
                    double amplitude = (settings.MaxY - settings.MinY) / 2.0;
                    double offsetY = (settings.MaxY + settings.MinY) / 2.0;

                    await responseStream.WriteAsync(new SignalPoint
                    {
                        X = currentX,
                        // Масштабируем синус: (значение * амплитуда) + смещение
                        Y = offsetY + (Math.Sin(currentX) * amplitude)
                    });

                    currentX += step;

                    if (currentX > settings.MaxX)
                    {
                        currentX = settings.MinX;
                    }

                }
            }


            finally
            {
                await readTask;
            }
        }
    }
}