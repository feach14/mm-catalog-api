namespace Catalog.Api.Features.Materials.ForAdmin;

using Core.CQRS;
using Dto;
using Microsoft.Extensions.Caching.Memory;

public sealed record UploadMaterialImageCommand(string FileName, byte[] Data, string ContentType)
    : ICommand<UploadMaterialImageCommandResult>;

public sealed class UploadMaterialImageCommandResult : CachedFileDto;

public class UploadMaterialImageCommandHandler(IMemoryCache memoryCache)
    : ICommandHandler<UploadMaterialImageCommand, UploadMaterialImageCommandResult>
{
    public Task<UploadMaterialImageCommandResult> Handle(UploadMaterialImageCommand command, CancellationToken ct)
    {
        var cachedFile = new UploadMaterialImageCommandResult
        {
            FileName = command.FileName,
            Data = command.Data,
            FileGuid = Guid.NewGuid(),
            ContentType = command.ContentType
        };

        memoryCache.Set(cachedFile.FileGuid, cachedFile, TimeSpan.FromDays(1));

        return Task.FromResult(cachedFile);
    }
}
