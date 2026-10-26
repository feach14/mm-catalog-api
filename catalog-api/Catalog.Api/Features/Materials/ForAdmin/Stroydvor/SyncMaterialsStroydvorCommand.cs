using System.Collections.Concurrent;
using Catalog.Api.Features.Materials.ForAdmin.Stroydvor.Dto;
using Catalog.Api.Services.History;
using Catalog.Api.Services.Stroydvor;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin.Stroydvor;

public sealed record SyncMaterialsStroydvorCommand(
    [property: Description("Id материалов")] int[] Ids) : ICommand<SyncMaterialsStroydvorCommandResult>;

public sealed record SyncMaterialsStroydvorCommandResult(
    [property: Description("Результаты синхронизации")] MaterialStroydvorSyncDto[] Items);

public sealed class SyncMaterialsStroydvorCommandValidator : AbstractValidator<SyncMaterialsStroydvorCommand>
{
    public SyncMaterialsStroydvorCommandValidator()
    {
        RuleFor(x => x.Ids)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указаны материалы для синхронизации")
            .Must(ids => ids.Length <= 100).WithMessage("За один запрос можно синхронизировать не более 100 материалов")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Id материалов не должны повторяться");
        RuleForEach(x => x.Ids).GreaterThan(0).WithMessage("Id материала должен быть больше 0");
    }
}

public sealed class SyncMaterialsStroydvorCommandHandler(
    CatalogDbContext dbContext,
    StroydvorProductClient productClient,
    ICatalogHistoryWriter historyWriter,
    TimeProvider timeProvider) : ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult>
{
    public async Task<SyncMaterialsStroydvorCommandResult> Handle(SyncMaterialsStroydvorCommand command, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .Include(x => x.MaterialThickness)
            .Include(x => x.MaterialSheetSize)
            .Where(x => command.Ids.Contains(x.Id))
            .ToArrayAsync(ct);
        if (materials.Length != command.Ids.Length)
            throw new BadHttpRequestException("Один из указанных материалов не найден.");
        foreach (var material in materials)
        {
            if (material.StroydvorProductId is not > 0)
                throw new BadHttpRequestException($"У материала #{material.Id} нет привязки к Стройдвору.");
            if (!StroydvorMaterialScope.IsEligible(material.CategoryId, material.MaterialManufacturerId,
                    material.MaterialTypeId, material.MaterialThickness.Value,
                    material.MaterialSheetSize.Height, material.MaterialSheetSize.Width, material.Name))
                throw new BadHttpRequestException($"Материал #{material.Id} не входит в выбранные категории ЛДСП 16 мм для Стройдвора.");
        }

        var reads = new ConcurrentDictionary<int, StroydvorProductReadResult>();
        await Parallel.ForEachAsync(materials, new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct }, async (material, token) =>
        {
            var result = await productClient.Read(material.StroydvorProductId!.Value, token);
            reads[material.Id] = ValidatePhysicalProperties(material, result);
        });

        var byId = materials.ToDictionary(x => x.Id);
        var items = new List<MaterialStroydvorSyncDto>();
        // Network calls finish before row updates. History and snapshot changes commit together.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        foreach (var id in command.Ids)
        {
            var material = byId[id];
            var read = reads[id];
            var price = read.Product?.Price ?? 0;
            var syncedAt = read.Success ? (DateTimeOffset?)timeProvider.GetUtcNow() : null;
            var inStock = read.Product?.InStock ?? false;
            var article = read.Product?.Article ?? material.StroydvorArticle;
            // A binding or identity edited during the supplier request must not receive the old product's price.
            var changed = await dbContext.Materials
                .Where(x => x.Id == id && x.StroydvorProductId == material.StroydvorProductId
                                      && x.CategoryId == material.CategoryId
                                      && x.MaterialManufacturerId == material.MaterialManufacturerId
                                      && x.MaterialTypeId == material.MaterialTypeId
                                      && x.MaterialThicknessId == material.MaterialThicknessId
                                      && x.MaterialSheetSizeId == material.MaterialSheetSizeId
                                      && x.Name == material.Name && x.Article == material.Article
                                      && x.StroydvorArticle == material.StroydvorArticle
                                      && x.StroydvorPrice == material.StroydvorPrice
                                      && x.StroydvorLastSyncAt == material.StroydvorLastSyncAt
                                      && x.StroydvorInStock == material.StroydvorInStock)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.StroydvorArticle, article)
                    .SetProperty(x => x.StroydvorPrice, price)
                    .SetProperty(x => x.StroydvorLastSyncAt, syncedAt)
                    .SetProperty(x => x.StroydvorInStock, inStock), ct);
            if (changed == 0)
            {
                var current = await dbContext.Materials.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
                items.Add(CurrentResult(id, current, "Материал, привязка или данные синхронизации изменились во время запроса. Повторите синхронизацию."));
                continue;
            }

            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, id,
                read.Success
                    ? $"Материал #{id} «{material.Name}»: синхронизирован товар Стройдвора #{material.StroydvorProductId}, артикул «{article ?? "нет"}», цена {price} руб. за лист, наличие: {(inStock ? "есть" : "нет")}."
                    : $"Материал #{id} «{material.Name}»: не удалось синхронизировать товар Стройдвора #{material.StroydvorProductId}. {read.Error} Цена Стройдвора сброшена в 0, дата синхронизации очищена.");
            items.Add(new MaterialStroydvorSyncDto(id, read.Success, read.Error,
                material.StroydvorProductId, article, price, syncedAt, inStock,
                StroydvorProductClient.ProductUrl(material.StroydvorProductId!.Value)));
        }
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SyncMaterialsStroydvorCommandResult(items.ToArray());
    }

    private static StroydvorProductReadResult ValidatePhysicalProperties(Material material, StroydvorProductReadResult read)
    {
        var product = read.Product;
        if (product is null) return read;
        if (product.ThicknessMm is not null && product.ThicknessMm != material.MaterialThickness.Value)
            return new StroydvorProductReadResult(null, "Толщина на карточке Стройдвора отличается от толщины нашего материала. Проверьте привязку.");
        if (product.Height is not null && product.Width is not null)
        {
            var size = material.MaterialSheetSize;
            if (!(product.Height == size.Height && product.Width == size.Width
                  || product.Height == size.Width && product.Width == size.Height))
                return new StroydvorProductReadResult(null, "Формат на карточке Стройдвора отличается от формата нашего материала. Проверьте привязку.");
        }
        return read;
    }

    private static MaterialStroydvorSyncDto CurrentResult(int id, Material? material, string error) =>
        new(id, false, error, material?.StroydvorProductId, material?.StroydvorArticle,
            material?.StroydvorPrice ?? 0, material?.StroydvorLastSyncAt, material?.StroydvorInStock ?? false,
            material?.StroydvorProductId is > 0 ? StroydvorProductClient.ProductUrl(material.StroydvorProductId.Value) : null);
}