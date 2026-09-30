using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories;

public sealed record GetAllMaterialCategoriesQuery : IQuery<GetAllMaterialCategoriesQueryResult>;

public sealed record GetAllMaterialCategoriesQueryResult(
    [property: Description("Список категорий")] GetAllMaterialCategoriesQueryItem[] Items);

public record GetAllMaterialCategoriesQueryItem(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Ссылка на внешний источник")] string ExternalLink,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol,
    [property: Description("Количество материалов в наличии у текущей категории")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии у текущей категории")] int MaterialsNotAnyCount
);

public class GetAllMaterialCategoriesQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialCategoriesQuery, GetAllMaterialCategoriesQueryResult>
{
    public async Task<GetAllMaterialCategoriesQueryResult> Handle(GetAllMaterialCategoriesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialCategories
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetAllMaterialCategoriesQueryItem
            (
                Id: x.Id,
                Name: x.Name,
                ExternalLink: x.ExternalLink,
                OrderByCol: x.OrderByCol,
                MaterialsAnyCount: x.Materials.Count(y => y.Count > 0),
                MaterialsNotAnyCount: x.Materials.Count(y => y.Count < 1)
            ))
            .ToArrayAsync(ct);

        return new GetAllMaterialCategoriesQueryResult(items);
    }
}