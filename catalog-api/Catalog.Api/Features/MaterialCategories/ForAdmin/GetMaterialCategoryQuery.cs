using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record GetMaterialCategoryQuery(int Id) : IQuery<GetMaterialCategoryQueryResult>;

public record GetMaterialCategoryQueryResult(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Ссылка на внешний источник")] string ExternalLink,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol,
    [property: Description("Признак: скрыть категорию на сайте")] bool HideOnSite);

public class GetMaterialCategoryQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialCategoryQuery, GetMaterialCategoryQueryResult>
{
    public async Task<GetMaterialCategoryQueryResult> Handle(GetMaterialCategoryQuery query, CancellationToken ct) =>
        await dbContext.MaterialCategories
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialCategoryQueryResult(
                Id: x.Id,
                Name: x.Name,
                ExternalLink: x.ExternalLink,
                OrderByCol: x.OrderByCol,
                HideOnSite: x.HideOnSite
            ))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Категория материалов с id={query.Id} не найдена");
}