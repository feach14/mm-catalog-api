using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record UpdateEdgeCommand(int Id, UpdateEdgeModel Model) : ICommand<UpdateEdgeCommandResult>;

public sealed record UpdateEdgeModel : EdgeModel;

public sealed class UpdateEdgeModelValidator : AbstractValidator<UpdateEdgeModel>
{
    public UpdateEdgeModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        Include(new EdgeModelValidator(dbContext));
        RuleFor(x => x)
            .MustAsync(async (_, ct) => await dbContext.Edges.AsNoTracking().AnyAsync(x => x.Id == RouteId(httpContextAccessor), ct))
            .WithMessage("Кромка не найдена").OverridePropertyName("Id");
        RuleFor(x => x.Image)
            .MustAsync(async (guid, ct) =>
            {
                var id = RouteId(httpContextAccessor);
                return await dbContext.EdgeImages.AsNoTracking().AnyAsync(
                    x => x.EdgeId == id && x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Original, ct)
                    || await dbContext.EdgeImageCache.AsNoTracking().AnyAsync(
                        x => x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Original, ct);
            }).When(x => x.Image is not null).WithMessage("Оригинальное изображение кромки не найдено или имеет другое назначение");
        RuleFor(x => x.Thumbnail240)
            .MustAsync(async (guid, ct) =>
            {
                var id = RouteId(httpContextAccessor);
                return await dbContext.EdgeImages.AsNoTracking().AnyAsync(
                    x => x.EdgeId == id && x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Thumbnail240, ct)
                    || await dbContext.EdgeImageCache.AsNoTracking().AnyAsync(
                        x => x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Thumbnail240, ct);
            }).When(x => x.Thumbnail240 is not null).WithMessage("Миниатюра кромки не найдена или имеет другое назначение");
    }

    private static int RouteId(IHttpContextAccessor accessor) =>
        int.TryParse(accessor.HttpContext?.Request.RouteValues["id"]?.ToString(), out var id) ? id : 0;
}

public sealed record UpdateEdgeCommandResult([property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeCommand, UpdateEdgeCommandResult>
{
    public async Task<UpdateEdgeCommandResult> Handle(UpdateEdgeCommand command, CancellationToken ct)
    {
        var edge = await dbContext.Edges.Include(x => x.Images).SingleAsync(x => x.Id == command.Id, ct);
        var model = command.Model;
        var original = edge.Images.SingleOrDefault(x => x.ImageType == EdgeImageTypeEnum.Original);
        var thumbnail = edge.Images.SingleOrDefault(x => x.ImageType == EdgeImageTypeEnum.Thumbnail240);
        var name = model.Name.Trim();
        var article = model.Article.Trim();
        var changes = new List<string>();
        AddChange(changes, "название", edge.Name, name);
        AddChange(changes, "артикул", edge.Article, article);
        AddChange(changes, "стоимость за погонный метр, руб.", edge.Price, model.Price);
        AddChange(changes, "производитель", edge.ManufacturerId, model.ManufacturerId);
        AddChange(changes, "толщина", edge.ThicknessId, model.ThicknessId);
        AddChange(changes, "единица продажи", edge.SaleUnitId, model.SaleUnitId);
        AddChange(changes, "складская позиция", edge.IsStockItem, model.IsStockItem);
        AddChange(changes, "остаток в метрах", edge.StockQuantity, model.StockQuantity);
        AddChange(changes, "оригинал", original?.Guid, model.Image);
        AddChange(changes, "миниатюра", thumbnail?.Guid, model.Thumbnail240);
        if (changes.Count == 0) return new UpdateEdgeCommandResult(true);

        edge.Name = name;
        edge.Article = article;
        edge.Price = model.Price;
        edge.ManufacturerId = model.ManufacturerId;
        edge.ThicknessId = model.ThicknessId;
        edge.SaleUnitId = model.SaleUnitId;
        edge.IsStockItem = model.IsStockItem;
        edge.StockQuantity = model.StockQuantity;
        Guid?[] replacedGuids =
        [
            await UpdateImage(edge, original, model.Image, EdgeImageTypeEnum.Original, ct),
            await UpdateImage(edge, thumbnail, model.Thumbnail240, EdgeImageTypeEnum.Thumbnail240, ct)
        ];
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, edge.Id,
            $"Кромка #{edge.Id} «{edge.Name}» изменена: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        foreach (var guid in replacedGuids.Where(x => x.HasValue)) memoryCache.Remove(("edge-image", guid!.Value));
        return new UpdateEdgeCommandResult(true);
    }

    private async Task<Guid?> UpdateImage(Edge edge, EdgeImage? current, Guid? requestedGuid, EdgeImageTypeEnum type, CancellationToken ct)
    {
        if (current?.Guid == requestedGuid) return null;
        if (current is not null) edge.Images.Remove(current);
        if (requestedGuid is null) return current?.Guid;
        var cached = await dbContext.EdgeImageCache.SingleAsync(x => x.Guid == requestedGuid && x.ImageType == type, ct);
        edge.Images.Add(new EdgeImage
        {
            EdgeId = edge.Id,
            Guid = cached.Guid,
            Type = cached.Type,
            Data = cached.Data,
            ImageType = cached.ImageType
        });
        dbContext.EdgeImageCache.Remove(cached);
        return current?.Guid;
    }

    private static void AddChange<T>(List<string> changes, string name, T oldValue, T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            changes.Add($"{name} «{oldValue?.ToString() ?? "нет"}» → «{newValue?.ToString() ?? "нет"}»");
    }
}