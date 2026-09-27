namespace Catalog.Api.Features.MaterialSheetSizes;

using Core.CQRS;
using Database;
using Dto;

public sealed record GetMaterialSheetSizeQuery(int Id) : IQuery<SheetSizeDto>;

public sealed class GetMaterialSheetSizeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizeQuery, SheetSizeDto>
{
    public async Task<SheetSizeDto> Handle(GetMaterialSheetSizeQuery query, CancellationToken ct) =>
        await dbContext.MaterialSheetSizes
            .Where(x => x.Id == query.Id)
            .Select(x => new SheetSizeDto(x.Id, x.Name, x.Height, x.Width, x.OrderByCol))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Размер материала с id={query.Id} не найден.");
}
