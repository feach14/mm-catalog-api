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
        var imageFromMemory = memoryCache.Get<GetMaterialImageQueryResult>(query.FileGuid);
        if (imageFromMemory != null)
            return imageFromMemory;

        var cachedFile = await dbContext.ImageCache
            .Where(x => x.Guid == query.FileGuid)
            .Select(x => new GetMaterialImageQueryResult(x.Data, x.Type))
            .FirstOrDefaultAsync(ct);
        if (cachedFile != null)
        {
            memoryCache.Set(query.FileGuid, cachedFile);
            return cachedFile;
        }

        var image = await dbContext.MaterialImages
                   .Where(x => x.Guid == query.FileGuid)
                   .Select(x => new GetMaterialImageQueryResult(x.Data, x.Type))
                   .FirstOrDefaultAsync(ct) ??
               throw new BadHttpRequestException($"Файл не найден. fileGuid={query.FileGuid}");

        memoryCache.Set(query.FileGuid, image);

        return image;
    }
}