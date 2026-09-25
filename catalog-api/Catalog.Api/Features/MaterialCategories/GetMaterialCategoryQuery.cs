using Catalog.Api.Features.MaterialCategories.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories;

public sealed record GetMaterialCategoryQuery(int Id) : IQuery<GetMaterialCategoryQueryResult>;

public sealed class GetMaterialCategoryQueryResult : MaterialCategoryForAdminDto;

public class GetMaterialCategoryQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialCategoryQuery, GetMaterialCategoryQueryResult>
{
    public async Task<GetMaterialCategoryQueryResult> Handle(GetMaterialCategoryQuery query, CancellationToken ct) =>
        await dbContext.MaterialCategories
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialCategoryQueryResult
            {
                Id = x.Id,
                Name = x.Name,
                ExternalLink = x.ExternalLink,
                OrderByCol = x.OrderByCol,
                MaterialsAnyCount = x.Materials.Count(y => y.Count > 0),
                MaterialsNotAnyCount = x.Materials.Count(y => y.Count < 1)
            })
            .FirstOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Категория материалов с id={query.Id} не найдена");
}
