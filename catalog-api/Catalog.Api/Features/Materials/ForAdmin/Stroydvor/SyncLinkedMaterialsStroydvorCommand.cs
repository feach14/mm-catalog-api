using Catalog.Api.Features.Materials.Helpers;
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
        var materials = await dbContext.Materials.AsNoTracking()
            .Where(x => x.StroydvorEnabled && x.StroydvorProductId > 0)
            .Select(x => new
            {
                x.Id,
                x.Name,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name
            })
            .ToArrayAsync(ct);
        var succeeded = 0;
        var failed = 0;

        await Parallel.ForEachAsync(materials, new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct }, async (material, token) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult>>();
            try
            {
                var result = await handler.Handle(new SyncMaterialsStroydvorCommand([material.Id]), token);
                if (result.Items[0].Success)
                    Interlocked.Increment(ref succeeded);
                else
                    Interlocked.Increment(ref failed);
            }
            catch (BadHttpRequestException error)
            {
                // A material can be removed or its binding changed after the initial selection.
                var materialName = MaterialNameHelper.GetFullName(material.Name, material.MaterialTypeName, material.Thickness,
                    material.Height, material.Width, material.ManufacturerName);
                logger.LogWarning(error, "Не удалось выполнить фоновую синхронизацию материала #{MaterialId} «{MaterialName}» со Стройдвором: {SyncError}",
                    material.Id, materialName, error.Message);
                Interlocked.Increment(ref failed);
            }
        });

        return new SyncLinkedMaterialsStroydvorCommandResult(materials.Length, succeeded, failed);
    }
}