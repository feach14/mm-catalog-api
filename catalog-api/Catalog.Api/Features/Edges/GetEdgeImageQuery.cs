using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Edges;

public sealed record GetEdgeImageQuery(
    [property: Description("Guid файла")] Guid FileGuid) : IQuery<GetEdgeImageQueryResult>;

public sealed record GetEdgeImageQueryResult(
    [property: Description("Содержимое файла")] byte[] Data,
    [property: Description("Тип файла")] string ContentType);

public class GetEdgeImageQueryHandler(CatalogDbContext dbContext, IMemoryCache memoryCache)
    : IQueryHandler<GetEdgeImageQuery, GetEdgeImageQueryResult>
{
    public async Task<GetEdgeImageQueryResult> Handle(GetEdgeImageQuery query, CancellationToken ct)
    {
        var cacheKey = ("edge-image", query.FileGuid);
        if (memoryCache.TryGetValue(cacheKey, out var cached) && cached is EdgeImageCache imageFromMemory)
            return new GetEdgeImageQueryResult(imageFromMemory.Data, imageFromMemory.Type);

        var cachedFile = await dbContext.EdgeImageCache
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Guid == query.FileGuid, ct);
        if (cachedFile is not null)
        {
            memoryCache.Set(cacheKey, cachedFile);
            return new GetEdgeImageQueryResult(cachedFile.Data, cachedFile.Type);
        }

        var image = await dbContext.EdgeImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Guid == query.FileGuid, ct);
        if (image is null)
            throw new BadHttpRequestException($"Файл не найден. fileGuid={query.FileGuid}");

        memoryCache.Set(cacheKey, new EdgeImageCache
        {
            Guid = image.Guid,
            FileName = image.Guid.ToString(),
            Type = image.Type,
            Data = image.Data,
            ImageType = image.ImageType
        });

        return new GetEdgeImageQueryResult(image.Data, image.Type);
    }
}