namespace Catalog.Api.Features.Materials;

using Core.CQRS;
using Database;
using ForAdmin.Dto;
using Microsoft.Extensions.Caching.Memory;

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
            new CachedFileDto
            {
                FileName = string.Empty,
                Data = image.Data,
                FileGuid = query.FileGuid,
                ContentType = image.ContentType
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromDays(1));

        return image;
    }
}
