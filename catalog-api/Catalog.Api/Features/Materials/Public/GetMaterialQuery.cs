namespace Catalog.Api.Features.Materials.Public;

using Core.CQRS;
using Database;
using Dto;
using MaterialSheetSizes.Dto;

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
                   SheetSize = new SheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width, x.MaterialSheetSize.OrderByCol),
                   Depth = x.Depth,
                   CommentOnMaterialIsRequired = x.CommentOnMaterialIsRequired,
                   Image = x.Images.Count != 0 ? x.Images.Select(g => g.Guid).First() : null,
                   CategoryId = x.CategoryId, 
                   CategoryName = x.Category.Name,
                   CategoryOrderBy = x.Category.OrderByCol,
                   OrderByCol = x.OrderByCol,
                   Count = x.Count
               })
               .SingleOrDefaultAsync(ct)
           ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}
