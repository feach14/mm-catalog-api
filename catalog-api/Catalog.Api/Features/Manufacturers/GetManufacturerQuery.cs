namespace Catalog.Api.Features.Manufacturers;

using Core.CQRS;
using Database;
using Dto;

public sealed record GetManufacturerQuery(int Id) : IQuery<ManufacturerDto>;

public sealed class GetManufacturerQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetManufacturerQuery, ManufacturerDto>
{
    public async Task<ManufacturerDto> Handle(GetManufacturerQuery query, CancellationToken ct) =>
        await dbContext.MaterialManufacturers
            .Where(x => x.Id == query.Id)
            .Select(x => new ManufacturerDto(x.Id, x.Name, x.OrderByCol))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Производитель с id={query.Id} не найден.");
}
