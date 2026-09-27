namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

using Core.CQRS;
using Database;

public sealed record UpdateMaterialSheetSizeCommand(int Id, SheetSizeModel Model) : ICommand<UpdateMaterialSheetSizeCommandResult>;
public sealed record UpdateMaterialSheetSizeCommandResult(bool Success);

public sealed class UpdateMaterialSheetSizeCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateMaterialSheetSizeCommand, UpdateMaterialSheetSizeCommandResult>
{
    public async Task<UpdateMaterialSheetSizeCommandResult> Handle(UpdateMaterialSheetSizeCommand command, CancellationToken ct)
    {
        var size = await dbContext.MaterialSheetSizes.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
                   ?? throw new BadHttpRequestException($"Размер материала с id={command.Id} не найден.");
        if (await dbContext.MaterialSheetSizes.AnyAsync(
                x => x.Id != command.Id && x.Name == command.Model.Name, ct))
            throw new BadHttpRequestException("Размер материала с таким названием уже существует.");

        if (await dbContext.MaterialSheetSizes.AnyAsync(
                x => x.Id != command.Id && x.Height == command.Model.Height && x.Width == command.Model.Width, ct))
            throw new BadHttpRequestException("Такой размер материала уже существует.");

        size.Name = command.Model.Name;
        size.Height = command.Model.Height;
        size.Width = command.Model.Width;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateMaterialSheetSizeCommandResult(true);
    }
}
