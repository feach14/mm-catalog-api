using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials;

public sealed record GetMaterialImageQuery(Guid FileGuid) : IQuery<GetMaterialImageQueryResult>;

public sealed record GetMaterialImageQueryResult(byte[] Data, string ContentType);

public class GetMaterialImageQueryHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : IQueryHandler<GetMaterialImageQuery, GetMaterialImageQueryResult>
{
    public async Task<GetMaterialImageQueryResult> Handle(GetMaterialImageQuery query, CancellationToken ct)
    {
        var imageFromMemory = memoryCache.Get<ImageCache>(query.FileGuid);
        if (imageFromMemory is not null)
            return new GetMaterialImageQueryResult(imageFromMemory.Data, imageFromMemory.Type);

        var cachedFile = await dbContext.ImageCache
            .Where(x => x.Guid == query.FileGuid)
            .FirstOrDefaultAsync(ct);
        if (cachedFile is not null)
        {
            memoryCache.Set(query.FileGuid, cachedFile);
            return new GetMaterialImageQueryResult(cachedFile.Data, cachedFile.Type);
        }

        var image = await dbContext.MaterialImages
            .Where(x => x.Guid == query.FileGuid)
            .FirstOrDefaultAsync(ct);

        if (image is null)
            throw new BadHttpRequestException($"Файл не найден. fileGuid={query.FileGuid}");

        memoryCache.Set(query.FileGuid, new ImageCache
        {
            Data = image.Data,
            Guid = image.Guid,
            Type = image.Type,
            FileName = image.Guid.ToString(),
            ImageType = image.ImageType
        });

        return new GetMaterialImageQueryResult(image.Data, image.Type);
    }
}