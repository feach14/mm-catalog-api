namespace Catalog.Api.Features.MaterialSheetSizes;

using Core.CQRS;
using Database;
using Dto;

public sealed record GetMaterialSheetSizesQuery : IQuery<GetMaterialSheetSizesQueryResult>;
public sealed record GetMaterialSheetSizesQueryResult(SheetSizeDto[] Items);

public sealed class GetMaterialSheetSizesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizesQuery, GetMaterialSheetSizesQueryResult>
{
    public async Task<GetMaterialSheetSizesQueryResult> Handle(GetMaterialSheetSizesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialSheetSizes
            .OrderBy(x => x.OrderByCol)
            .Select(x => new SheetSizeDto(x.Id, x.Name, x.Height, x.Width, x.OrderByCol))
            .ToArrayAsync(ct);
        return new GetMaterialSheetSizesQueryResult(items);
    }
}
