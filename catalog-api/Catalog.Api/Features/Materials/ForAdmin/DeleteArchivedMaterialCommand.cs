using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record DeleteArchivedMaterialCommand(int Id) : ICommand<DeleteMaterialCommandResult>;

public class DeleteArchivedMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache, ICatalogHistoryWriter historyWriter) : ICommandHandler<DeleteArchivedMaterialCommand, DeleteMaterialCommandResult>
{
    public async Task<DeleteMaterialCommandResult> Handle(DeleteArchivedMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.ArchivedMaterials
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не найден.");

        var imageGuids = material.Images.Select(x => x.Guid).ToArray();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.Material,
            material.Id,
            $"Удалён материал #{material.Id} «{material.Name}». Перед удалением: артикул {material.Article}, категория #{material.CategoryId}, производитель #{material.MaterialManufacturerId}, тип материала #{material.MaterialTypeId}, размер #{material.MaterialSheetSizeId}, количество {material.Count}, цена {material.Price}, изображения {(imageGuids.Length == 0 ? "нет" : string.Join(", ", imageGuids))}.");
        dbContext.ArchivedMaterials.Remove(material);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        foreach (var imageGuid in imageGuids)
            memoryCache.Remove(imageGuid);

        return new DeleteMaterialCommandResult(true);
    }
}