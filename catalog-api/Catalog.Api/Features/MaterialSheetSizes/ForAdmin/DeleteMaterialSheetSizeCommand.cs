namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

using Core.CQRS;
using Database;

public sealed record DeleteMaterialSheetSizeCommand(int Id) : ICommand<DeleteMaterialSheetSizeCommandResult>;
public sealed record DeleteMaterialSheetSizeCommandResult(bool Success);

public sealed class DeleteMaterialSheetSizeCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteMaterialSheetSizeCommand, DeleteMaterialSheetSizeCommandResult>
{
    public async Task<DeleteMaterialSheetSizeCommandResult> Handle(DeleteMaterialSheetSizeCommand command, CancellationToken ct)
    {
        var size = await dbContext.MaterialSheetSizes.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
                   ?? throw new BadHttpRequestException($"Размер материала с id={command.Id} не найден.");
        if (await dbContext.Materials.AnyAsync(x => x.MaterialSheetSizeId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить размер, используемый материалами.");

        dbContext.MaterialSheetSizes.Remove(size);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteMaterialSheetSizeCommandResult(true);
    }
}
