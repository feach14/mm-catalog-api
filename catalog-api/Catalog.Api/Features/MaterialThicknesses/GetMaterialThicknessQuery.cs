using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses;

public sealed record GetMaterialThicknessQuery(int Id) : IQuery<MaterialThicknessDto>;

public sealed class GetMaterialThicknessQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialThicknessQuery, MaterialThicknessDto>
{
    public async Task<MaterialThicknessDto> Handle(GetMaterialThicknessQuery query, CancellationToken ct)
    {
        var item = await dbContext.MaterialThicknesses
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new MaterialThicknessDto(x.Id, x.Name, x.Value))
            .SingleOrDefaultAsync(ct);
        return item ?? throw new BadHttpRequestException($"Толщина материала с id={query.Id} не найдена.");
    }
}