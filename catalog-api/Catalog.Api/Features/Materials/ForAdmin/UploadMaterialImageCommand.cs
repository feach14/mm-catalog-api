using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UploadMaterialImageCommand(string FileName, byte[] Data, string ContentType)
    : ICommand<UploadMaterialImageCommandResult>;

public sealed class UploadMaterialImageCommandResult : CachedFileDto;

public class UploadMaterialImageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UploadMaterialImageCommand, UploadMaterialImageCommandResult>
{
    public async Task<UploadMaterialImageCommandResult> Handle(UploadMaterialImageCommand command, CancellationToken ct)
    {
        var cachedFile = new UploadMaterialImageCommandResult
        {
            FileName = command.FileName,
            FileGuid = Guid.NewGuid(),
            ContentType = command.ContentType
        };

        await dbContext.ImageCache.AddAsync(new ImageCache
        {
            Guid = cachedFile.FileGuid,
            FileName = cachedFile.FileName,
            Type = cachedFile.ContentType,
            Data = command.Data
        }, ct);
        await dbContext.SaveChangesAsync(ct);

        return cachedFile;
    }
}