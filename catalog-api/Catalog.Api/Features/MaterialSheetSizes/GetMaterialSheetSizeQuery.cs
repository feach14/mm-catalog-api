using Catalog.Api.Features.MaterialSheetSizes.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes;

public sealed record GetMaterialSheetSizeQuery(int Id) : IQuery<SheetSizeDto>;

public sealed class GetMaterialSheetSizeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizeQuery, SheetSizeDto>
{
    public async Task<SheetSizeDto> Handle(GetMaterialSheetSizeQuery query, CancellationToken ct) =>
        await dbContext.MaterialSheetSizes
            .Where(x => x.Id == query.Id)
            .Select(x => new SheetSizeDto(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Размер материала с id={query.Id} не найден.");
}