using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin.Stroydvor;

public sealed record SyncLinkedMaterialsStroydvorCommand : ICommand<SyncLinkedMaterialsStroydvorCommandResult>;

public sealed record SyncLinkedMaterialsStroydvorCommandResult(
    [property: Description("Всего связанных материалов")] int Total,
    [property: Description("Успешно синхронизировано")] int Succeeded,
    [property: Description("Не удалось синхронизировать")] int Failed);

public sealed class SyncLinkedMaterialsStroydvorCommandHandler(
    CatalogDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    ILogger<SyncLinkedMaterialsStroydvorCommandHandler> logger)
    : ICommandHandler<SyncLinkedMaterialsStroydvorCommand, SyncLinkedMaterialsStroydvorCommandResult>
{
    public async Task<SyncLinkedMaterialsStroydvorCommandResult> Handle(SyncLinkedMaterialsStroydvorCommand command, CancellationToken ct)
    {
        var ids = await dbContext.Materials.AsNoTracking()
            .Where(x => x.StroydvorProductId > 0)
            .Select(x => x.Id)
            .ToArrayAsync(ct);
        var succeeded = 0;
        var failed = 0;

        await Parallel.ForEachAsync(ids, new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct }, async (id, token) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult>>();
            try
            {
                var result = await handler.Handle(new SyncMaterialsStroydvorCommand([id]), token);
                if (result.Items[0].Success)
                    Interlocked.Increment(ref succeeded);
                else
                    Interlocked.Increment(ref failed);
            }
            catch (BadHttpRequestException error)
            {
                // A material can be removed or its binding changed after the initial selection.
                logger.LogWarning(error, "Не удалось выполнить фоновую синхронизацию материала #{MaterialId} со Стройдвором: {SyncError}", id, error.Message);
                Interlocked.Increment(ref failed);
            }
        });

        return new SyncLinkedMaterialsStroydvorCommandResult(ids.Length, succeeded, failed);
    }
}