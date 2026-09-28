using Catalog.Api.Features.Manufacturers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers;

public sealed record GetManufacturerQuery(int Id) : IQuery<ManufacturerDto>;

public sealed class GetManufacturerQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetManufacturerQuery, ManufacturerDto>
{
    public async Task<ManufacturerDto> Handle(GetManufacturerQuery query, CancellationToken ct) =>
        await dbContext.MaterialManufacturers
            .Where(x => x.Id == query.Id)
            .Select(x => new ManufacturerDto(
                x.Id,
                x.Name,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Производитель с id={query.Id} не найден.");
}
