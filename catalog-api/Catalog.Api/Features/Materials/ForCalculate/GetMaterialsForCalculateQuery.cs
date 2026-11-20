// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialsForCalculateQuery(
    [property: Description("Поиск по словам в названии, артикуле и характеристиках, до 250 символов"), FromQuery] string? Search = null)
    : IQuery<GetMaterialsForCalculateQueryResult>;

public sealed class GetMaterialsForCalculateQueryValidator : AbstractValidator<GetMaterialsForCalculateQuery>
{
    public GetMaterialsForCalculateQueryValidator()
    {
        RuleFor(x => x.Search)
            .Must(search => search is null || search.Trim().Length <= 250)
            .WithMessage("Поисковая строка не должна превышать 250 символов");
    }
}

public sealed record GetMaterialsForCalculateQueryResult(
    [property: Description("Материалы")] GetMaterialsForCalculateQueryItem[] Items);

public sealed record GetMaterialsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Полное название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Категория")] MaterialCategoryForCalculateDto Category);

public sealed record MaterialCategoryForCalculateDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Порядок сортировки")] int OrderByCol);

public sealed class GetMaterialsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult>
{
    public async Task<GetMaterialsForCalculateQueryResult> Handle(GetMaterialsForCalculateQuery query, CancellationToken ct)
    {
        var materials = MaterialSearchQuery.Apply(dbContext.Materials.AsNoTracking(), query.Search);
        var rows = await materials
            .OrderBy(x => x.OrderByCol)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Article,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name,
                Category = new MaterialCategoryForCalculateDto(x.CategoryId, x.Category.Name, x.Category.OrderByCol)
            })
            .ToArrayAsync(ct);
        var items = rows.Select(x => new GetMaterialsForCalculateQueryItem(x.Id,
            MaterialNameHelper.GetFullName(x.Name, x.MaterialTypeName, x.Thickness,
                x.Height, x.Width, x.ManufacturerName), x.Article, x.Category)).ToArray();

        return new GetMaterialsForCalculateQueryResult(items);
    }
}