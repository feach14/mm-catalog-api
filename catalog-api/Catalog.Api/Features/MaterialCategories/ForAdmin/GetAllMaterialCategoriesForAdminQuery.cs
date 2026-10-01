// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record GetAllMaterialCategoriesForAdminQuery : IQuery<GetAllMaterialCategoriesForAdminQueryResult>;

public sealed record GetAllMaterialCategoriesForAdminQueryResult(
    [property: Description("Список категорий")] GetAllMaterialCategoriesForAdminQueryItem[] Items);

public record GetAllMaterialCategoriesForAdminQueryItem(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Ссылка на внешний источник")] string ExternalLink,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol,
    [property: Description("Признак: скрыть категорию на сайте")] bool HideOnSite,
    [property: Description("Количество материалов в наличии у текущей категории")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии у текущей категории")] int MaterialsNotAnyCount
);

public class GetAllMaterialCategoriesQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialCategoriesForAdminQuery, GetAllMaterialCategoriesForAdminQueryResult>
{
    public async Task<GetAllMaterialCategoriesForAdminQueryResult> Handle(GetAllMaterialCategoriesForAdminQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialCategories
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetAllMaterialCategoriesForAdminQueryItem
            (
                Id: x.Id,
                Name: x.Name,
                ExternalLink: x.ExternalLink,
                OrderByCol: x.OrderByCol,
                HideOnSite: x.HideOnSite,
                MaterialsAnyCount: x.Materials.Count(y => y.Count > 0),
                MaterialsNotAnyCount: x.Materials.Count(y => y.Count < 1)
            ))
            .ToArrayAsync(ct);

        return new GetAllMaterialCategoriesForAdminQueryResult(items);
    }
}