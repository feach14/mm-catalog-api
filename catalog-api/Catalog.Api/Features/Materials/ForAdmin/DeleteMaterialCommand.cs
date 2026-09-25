namespace Catalog.Api.Features.Materials.ForAdmin;

using Core.CQRS;
using Database;

public sealed record DeleteMaterialCommand(int Id) : ICommand<DeleteMaterialCommandResult>;

public sealed record DeleteMaterialCommandResult(
    [property:Description("Успех операции")] bool Success);

public class DeleteMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult>
{
    public async Task<DeleteMaterialCommandResult> Handle(DeleteMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.Materials
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.Deleted, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не найден или уже удалён.");

        material.Name += " (удалён)";
        material.Deleted = true;
        material.Images.Clear();
        dbContext.Materials.Update(material);
        await dbContext.SaveChangesAsync(ct);

        return new DeleteMaterialCommandResult(true);
    }
}
