using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record DeleteMaterialCommand(int Id) : ICommand<DeleteMaterialCommandResult>;

public sealed record DeleteMaterialCommandResult(
    [property:Description("Успех операции")] bool Success);

public class DeleteMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult>
{
    public async Task<DeleteMaterialCommandResult> Handle(DeleteMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.Materials
            .Include(x => x.Images)
            .Where(x => x.Id == command.Id)
            .FirstAsync(ct);

        material.Name += " (удалён)";
        material.Deleted = true;
        material.Images.Clear();
        dbContext.Materials.Update(material);
        await dbContext.SaveChangesAsync(ct);

        return new DeleteMaterialCommandResult(true);
    }
}