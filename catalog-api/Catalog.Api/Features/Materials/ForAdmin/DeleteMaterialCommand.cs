namespace Catalog.Api.Features.Materials.ForAdmin;

using Core.CQRS;
using Database;
using Microsoft.Extensions.Caching.Memory;

public sealed record DeleteMaterialCommand(int Id) : ICommand<DeleteMaterialCommandResult>;

public sealed record DeleteMaterialCommandResult(
    [property:Description("Успех операции")] bool Success);

public class DeleteMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult>
{
    public async Task<DeleteMaterialCommandResult> Handle(DeleteMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.Materials
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.Deleted, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не найден или уже удалён.");

        var imageGuids = material.Images.Select(x => x.Guid).ToArray();

        material.Name += " (удалён)";
        material.Deleted = true;
        material.Images.Clear();
        dbContext.Materials.Update(material);
        await dbContext.SaveChangesAsync(ct);

        foreach (var imageGuid in imageGuids)
            memoryCache.Remove(imageGuid);

        return new DeleteMaterialCommandResult(true);
    }
}
