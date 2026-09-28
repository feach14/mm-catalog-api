using Catalog.Api.Features.MaterialSheetSizes.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes;

public sealed record GetMaterialSheetSizesQuery : IQuery<GetMaterialSheetSizesQueryResult>;
public sealed record GetMaterialSheetSizesQueryResult(SheetSizeDto[] Items);

public sealed class GetMaterialSheetSizesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizesQuery, GetMaterialSheetSizesQueryResult>
{
    public async Task<GetMaterialSheetSizesQueryResult> Handle(GetMaterialSheetSizesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialSheetSizes
            .OrderBy(x => x.OrderByCol)
            .Select(x => new SheetSizeDto(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .ToArrayAsync(ct);
        return new GetMaterialSheetSizesQueryResult(items);
    }
}