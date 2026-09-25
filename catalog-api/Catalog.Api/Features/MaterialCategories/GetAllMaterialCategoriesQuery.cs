using Catalog.Api.Features.MaterialCategories.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories;

public sealed record GetAllMaterialCategoriesQuery : IQuery<GetAllMaterialCategoriesQueryResult>;

public sealed record GetAllMaterialCategoriesQueryResult(
    [property:Description("Список категорий")] MaterialCategoryForAdminDto[] Items);

public class GetAllMaterialCategoriesQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialCategoriesQuery, GetAllMaterialCategoriesQueryResult>
{
    public async Task<GetAllMaterialCategoriesQueryResult> Handle(GetAllMaterialCategoriesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialCategories
            .OrderBy(x => x.OrderByCol)
            .Select(x => new MaterialCategoryForAdminDto
            {
                Id = x.Id,
                Name = x.Name,
                ExternalLink = x.ExternalLink,
                OrderByCol = x.OrderByCol,
                MaterialsAnyCount = x.Materials.Count(y => y.Count > 0),
                MaterialsNotAnyCount = x.Materials.Count(y => y.Count < 1)
            })
            .ToArrayAsync(ct);
        
        return new GetAllMaterialCategoriesQueryResult(items);
    }
}
