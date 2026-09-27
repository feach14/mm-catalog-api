using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record CreateMaterialSheetSizeCommand(SheetSizeModel Model) : ICommand<CreateMaterialSheetSizeCommandResult>;
public sealed record CreateMaterialSheetSizeCommandResult(int Id);

public sealed class CreateMaterialSheetSizeCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<CreateMaterialSheetSizeCommand, CreateMaterialSheetSizeCommandResult>
{
    public async Task<CreateMaterialSheetSizeCommandResult> Handle(CreateMaterialSheetSizeCommand command, CancellationToken ct)
    {
        if (await dbContext.MaterialSheetSizes.AnyAsync(x => x.Name == command.Model.Name, ct))
            throw new BadHttpRequestException("Размер материала с таким названием уже существует.");

        if (await dbContext.MaterialSheetSizes.AnyAsync(
                x => x.Height == command.Model.Height && x.Width == command.Model.Width, ct))
            throw new BadHttpRequestException("Такой размер материала уже существует.");

        var maxOrderByCol = await dbContext.MaterialSheetSizes.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
        var size = new MaterialSheetSize
        {
            Name = command.Model.Name,
            Height = command.Model.Height,
            Width = command.Model.Width,
            OrderByCol = maxOrderByCol + 1
        };
        dbContext.MaterialSheetSizes.Add(size);
        await dbContext.SaveChangesAsync(ct);
        return new CreateMaterialSheetSizeCommandResult(size.Id);
    }
}