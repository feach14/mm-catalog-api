// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

[Description("Материалы по Id")]
public sealed record GetMaterialsByIdsForCalculateQuery(
    [property: Description("Id материалов")] int[] MaterialIds)
    : IQuery<GetMaterialsByIdsForCalculateQueryResult>;

public sealed class GetMaterialsByIdsForCalculateQueryValidator : AbstractValidator<GetMaterialsByIdsForCalculateQuery>
{
    public GetMaterialsByIdsForCalculateQueryValidator()
    {
        RuleFor(x => x.MaterialIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не выбраны материалы")
            .Must(ids => ids.All(id => id > 0)).WithMessage("Id материалов должны быть больше нуля")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Id материалов не должны повторяться");
    }
}

[Description("Материалы")]
public sealed record GetMaterialsByIdsForCalculateQueryResult(
    [property: Description("Материалы")] GetMaterialsByIdsForCalculateQueryItem[] Items);

[Description("Материал")]
public sealed record GetMaterialsByIdsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Комментарии для клиента")] string[] ClientComments,
    [property: Description("Материал есть в наличии")] bool InStock,
    [property: Description("Признак цены поставщика; true также при выключенной связи")] bool IsSupplierPrice);

public sealed class GetMaterialsByIdsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsByIdsForCalculateQuery, GetMaterialsByIdsForCalculateQueryResult>
{
    public async Task<GetMaterialsByIdsForCalculateQueryResult> Handle(GetMaterialsByIdsForCalculateQuery query, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .Where(x => query.MaterialIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Article,
                x.CategoryId,
                CategoryName = x.Category.Name,
                x.OrderByCol,
                InStock = x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                IsSupplierPrice = !x.StroydvorEnabled || x.StroydvorPrice > 0
            })
            .Concat(dbContext.ArchivedMaterials.AsNoTracking()
                .Where(x => query.MaterialIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Article,
                    x.CategoryId,
                    CategoryName = x.Category.Name,
                    x.OrderByCol,
                    InStock = x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                    IsSupplierPrice = !x.StroydvorEnabled || x.StroydvorPrice > 0
                }))
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);
        var materialIds = materials.Select(material => material.Id).ToArray();
        var comments = await dbContext.MaterialComments.AsNoTracking()
            .Where(comment => comment.IsForClient
                && materialIds.Contains((comment.MaterialId ?? comment.ArchivedMaterialId)!.Value))
            .OrderBy(comment => comment.Id)
            .Select(comment => new { MaterialId = (comment.MaterialId ?? comment.ArchivedMaterialId)!.Value, comment.Text })
            .ToArrayAsync(ct);
        var commentsByMaterial = comments.ToLookup(comment => comment.MaterialId, comment => comment.Text);
        var items = materials.Select(x => new GetMaterialsByIdsForCalculateQueryItem
            (
                x.Id,
                x.Name,
                x.Article,
                new PropertyDto(x.CategoryId, x.CategoryName),
                commentsByMaterial[x.Id].ToArray(),
                x.InStock,
                x.IsSupplierPrice
            )).ToArray();

        return new GetMaterialsByIdsForCalculateQueryResult(items);
    }
}