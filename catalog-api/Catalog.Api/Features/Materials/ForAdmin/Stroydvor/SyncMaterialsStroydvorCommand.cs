using System.Collections.Concurrent;
using Catalog.Api.Features.Materials.ForAdmin.Stroydvor.Dto;
using Catalog.Api.Features.Materials.Helpers;
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
    TimeProvider timeProvider,
    ILogger<SyncMaterialsStroydvorCommandHandler> logger) : ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult>
{
    public async Task<SyncMaterialsStroydvorCommandResult> Handle(SyncMaterialsStroydvorCommand command, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .Include(x => x.MaterialThickness)
            .Include(x => x.MaterialSheetSize)
            .Include(x => x.MaterialType)
            .Include(x => x.MaterialManufacturer)
            .Where(x => command.Ids.Contains(x.Id))
            .ToArrayAsync(ct);
        if (materials.Length != command.Ids.Length)
        {
            logger.LogError("Не удалось синхронизировать материалы со Стройдвором: один из указанных актуальных материалов не найден. Id материалов: {MaterialIds}",
                string.Join(", ", command.Ids));
            throw new BadHttpRequestException("Один из указанных актуальных материалов не найден.");
        }
        foreach (var material in materials)
        {
            if (material.StroydvorProductId is not > 0)
            {
                var error = $"У материала #{material.Id} «{MaterialNameHelper.GetFullName(material)}» нет привязки к Стройдвору.";
                LogSyncFailure(material, error);
                throw new BadHttpRequestException(error);
            }
            if (!StroydvorMaterialScope.IsEligible(material.MaterialManufacturerId, material.MaterialType.Name,
                    material.MaterialSheetSize.Height, material.MaterialSheetSize.Width))
            {
                var error = $"Материал #{material.Id} «{MaterialNameHelper.GetFullName(material)}» не относится к ЛДСП или ЛМДФ выбранных производителей и форматов для Стройдвора.";
                LogSyncFailure(material, error);
                throw new BadHttpRequestException(error);
            }
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
            var materialName = MaterialNameHelper.GetFullName(material);
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
                                      && x.MaterialType.Name == material.MaterialType.Name
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
                var error = $"Материал #{id} «{materialName}», привязка или данные синхронизации изменились во время запроса. Повторите синхронизацию.";
                LogSyncFailure(material, error, read.Product);
                items.Add(CurrentResult(id, current, error));
                continue;
            }

            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, id,
                read.Success
                    ? $"Материал #{id} «{materialName}»: синхронизирован товар Стройдвора #{material.StroydvorProductId}, артикул «{article ?? "нет"}», цена {price} руб. за лист, наличие: {(inStock ? "есть" : "нет")}."
                    : $"Материал #{id} «{materialName}»: не удалось синхронизировать товар Стройдвора #{material.StroydvorProductId}. {read.Error} Цена Стройдвора сброшена в 0, дата синхронизации очищена.");
            var syncError = read.Success ? null : $"Материал #{id} «{materialName}»: {read.Error}";
            items.Add(new MaterialStroydvorSyncDto(id, read.Success, syncError,
                material.StroydvorProductId, article, price, syncedAt, inStock,
                StroydvorProductClient.ProductUrl(material.StroydvorProductId!.Value)));
        }
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SyncMaterialsStroydvorCommandResult(items.ToArray());
    }

    private StroydvorProductReadResult ValidatePhysicalProperties(Material material, StroydvorProductReadResult read)
    {
        var product = read.Product;
        var error = read.Error;
        var logDetails = read.LogDetails;
        if (product is not null)
        {
            var size = material.MaterialSheetSize;
            if (product.MaterialType != StroydvorMaterialScope.GetMaterialType(material.MaterialType.Name))
            {
                error = $"Тип материала на карточке Стройдвора — «{StroydvorMaterialScope.GetMaterialTypeName(product.MaterialType)}», у нашего материала — «{material.MaterialType.Name}». Проверьте привязку.";
                logDetails = $"Тип: у нас «{material.MaterialType.Name}», в Стройдворе «{StroydvorMaterialScope.GetMaterialTypeName(product.MaterialType)}».";
            }
            else if (product.ThicknessMm is not null && product.ThicknessMm != material.MaterialThickness.Value)
            {
                error = "Толщина на карточке Стройдвора отличается от толщины нашего материала. Проверьте привязку.";
                logDetails = $"Толщина: у нас {material.MaterialThickness.Value} мм, в Стройдворе {product.ThicknessMm} мм.";
            }
            else if (product.Height is not null && product.Width is not null
                     && !(product.Height == size.Height && product.Width == size.Width
                          || product.Height == size.Width && product.Width == size.Height))
            {
                error = "Формат на карточке Стройдвора отличается от формата нашего материала. Проверьте привязку.";
                logDetails = $"Формат: у нас {size.Height}×{size.Width} мм, в Стройдворе {product.Height}×{product.Width} мм.";
            }
            else
                return read;
        }

        LogSyncFailure(material, logDetails is null ? error : $"{error} {logDetails}", product);
        return product is null ? read : new StroydvorProductReadResult(null, error, logDetails);
    }

    private void LogSyncFailure(Material material, string? error, StroydvorProductData? product = null) =>
        logger.LogError(
            "Не удалось синхронизировать материал #{MaterialId} «{MaterialName}», артикул «{MaterialArticle}», с товаром Стройдвора #{StroydvorProductId}. Причина: {SyncError} " +
            "Тип: у нас «{MaterialType}», в Стройдворе «{StroydvorMaterialType}». " +
            "Толщина: у нас {MaterialThickness} мм, в Стройдворе {StroydvorThickness} мм. " +
            "Формат: у нас {MaterialHeight}×{MaterialWidth} мм, в Стройдворе {StroydvorHeight}×{StroydvorWidth} мм. " +
            "Карточка Стройдвора «{StroydvorProductName}», артикул «{StroydvorArticle}».",
            material.Id, MaterialNameHelper.GetFullName(material), material.Article, material.StroydvorProductId, error,
            material.MaterialType.Name, product is null ? null : StroydvorMaterialScope.GetMaterialTypeName(product.MaterialType),
            material.MaterialThickness.Value, product?.ThicknessMm,
            material.MaterialSheetSize.Height, material.MaterialSheetSize.Width, product?.Height, product?.Width,
            product?.Name, product?.Article);

    private static MaterialStroydvorSyncDto CurrentResult(int id, Material? material, string error) =>
        new(id, false, error, material?.StroydvorProductId, material?.StroydvorArticle,
            material?.StroydvorPrice ?? 0, material?.StroydvorLastSyncAt, material?.StroydvorInStock ?? false,
            material?.StroydvorProductId is > 0 ? StroydvorProductClient.ProductUrl(material.StroydvorProductId.Value) : null);
}