using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.Public.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetMaterialQuery(int Id) : IQuery<GetMaterialQueryResult>;

public class GetMaterialQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialQuery, GetMaterialQueryResult>
{
    public async Task<GetMaterialQueryResult> Handle(GetMaterialQuery query, CancellationToken ct)
        => await dbContext.Materials
               .Include(x => x.Category)
               .Where(x => x.Id == query.Id)
               .Select(x => new GetMaterialQueryResult
               {
                   Id = x.Id,
                   Name = x.Name,
                   Article = x.Article,
                   SheetSize = new MaterialSheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                   Manufacturer = new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                   Depth = x.Depth,
                   CommentOnMaterialIsRequired = x.CommentOnMaterialIsRequired,
                   Image = x.Images.Count != 0 ? x.Images.Select(g => g.Guid).First() : null,
                   Category = new CategoryDto(x.CategoryId, x.Category.Name),
                   OrderByCol = x.OrderByCol,
                   Count = x.Count
               })
               .SingleOrDefaultAsync(ct)
           ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}