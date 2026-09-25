using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials;

public sealed record GetMaterialImageQuery(Guid FileGuid) : IQuery<GetMaterialImageQueryResult>;

public sealed record GetMaterialImageQueryResult(byte[] Data, string ContentType);

public class GetMaterialImageQueryHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : IQueryHandler<GetMaterialImageQuery, GetMaterialImageQueryResult>
{
    public async Task<GetMaterialImageQueryResult> Handle(GetMaterialImageQuery query, CancellationToken ct)
    {
        var cachedFile = memoryCache.Get<CachedFileDto>(query.FileGuid);
        if (cachedFile != null)
            return new GetMaterialImageQueryResult(cachedFile.Data, cachedFile.ContentType);
            
        var image = await dbContext.MaterialImages
                   .Where(x => x.Guid == query.FileGuid)
                   .Select(x => new GetMaterialImageQueryResult(x.Data, x.Type))
                   .FirstOrDefaultAsync(ct) ??
               throw new BadHttpRequestException($"Файл не найден. fileGuid={query.FileGuid}");

        memoryCache.Set(query.FileGuid, 
            new CachedFileDto(string.Empty, image.Data, query.FileGuid, image.ContentType),
            absoluteExpirationRelativeToNow: TimeSpan.FromDays(1));

        return image;
    }
}
