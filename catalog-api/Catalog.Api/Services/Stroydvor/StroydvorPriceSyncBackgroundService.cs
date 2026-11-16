using System.Data.Common;
using Catalog.Api.Features.Materials.ForAdmin.Stroydvor;
using Core.CQRS;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Services.Stroydvor;

public sealed class StroydvorPriceSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<StroydvorPriceSyncOptions> options,
    TimeProvider timeProvider,
    ILogger<StroydvorPriceSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StroydvorPriceSyncOptions settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Фоновое обновление цен Стройдвора отключено");
            return;
        }

        StroydvorCronSchedule.TryParse(settings.Cron, out StroydvorCronSchedule? schedule);
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset? nextRun = schedule!.GetNextOccurrence(timeProvider.GetUtcNow(), timeZone);
            if (nextRun is null)
            {
                logger.LogError("Для расписания обновления цен Стройдвора {Cron} не найден следующий запуск", settings.Cron);
                return;
            }

            logger.LogInformation("Следующее обновление цен Стройдвора: {NextRun}; cron: {Cron}, часовой пояс: {TimeZoneId}",
                TimeZoneInfo.ConvertTime(nextRun.Value, timeZone), settings.Cron, settings.TimeZoneId);
            // Short waits also support sparse schedules and recheck the clock after a system time adjustment.
            while (nextRun.Value > timeProvider.GetUtcNow())
            {
                TimeSpan delay = nextRun.Value - timeProvider.GetUtcNow();
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay < TimeSpan.FromMinutes(1) ? delay : TimeSpan.FromMinutes(1), timeProvider, stoppingToken);
            }

            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<
                    ICommandHandler<SyncLinkedMaterialsStroydvorCommand, SyncLinkedMaterialsStroydvorCommandResult>>();
                logger.LogInformation("Начато фоновое обновление цен Стройдвора");
                var result = await handler.Handle(new SyncLinkedMaterialsStroydvorCommand(), stoppingToken);
                logger.LogInformation("Фоновое обновление цен Стройдвора завершено. Всего: {Total}, обновлено: {Succeeded}, с ошибками: {Failed}",
                    result.Total, result.Succeeded, result.Failed);
            }
            catch (DbException error)
            {
                // A database outage must not stop the API; the next cron occurrence retries the linked materials.
                logger.LogError(error, "Ошибка БД при фоновом обновлении цен Стройдвора. Повтор по расписанию");
            }
            catch (DbUpdateException error)
            {
                // Each material has its own transaction, so a failed write does not undo completed materials.
                logger.LogError(error, "Не удалось сохранить фоновое обновление цен Стройдвора. Повтор по расписанию");
            }
        }
    }
}