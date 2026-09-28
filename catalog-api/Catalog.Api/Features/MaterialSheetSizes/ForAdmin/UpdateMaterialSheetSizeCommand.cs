using Catalog.Api.Features.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record UpdateMaterialSheetSizeCommand(int Id, SheetSizeModel Model) : ICommand<UpdateMaterialSheetSizeCommandResult>;
public sealed record UpdateMaterialSheetSizeCommandResult(bool Success);

public sealed class UpdateMaterialSheetSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
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

        var changes = new List<string>();
        if (size.Name != command.Model.Name)
            changes.Add($"название «{size.Name}» → «{command.Model.Name}»");
        if (size.Height != command.Model.Height)
            changes.Add($"высота {size.Height} → {command.Model.Height}");
        if (size.Width != command.Model.Width)
            changes.Add($"ширина {size.Width} → {command.Model.Width}");
        if (size.ShowInFilters != command.Model.ShowInFilters)
            changes.Add($"показывать в фильтрах «{(size.ShowInFilters ? "Да" : "Нет")}» → «{(command.Model.ShowInFilters ? "Да" : "Нет")}»");

        if (changes.Count == 0)
            return new UpdateMaterialSheetSizeCommandResult(true);

        size.Name = command.Model.Name;
        size.Height = command.Model.Height;
        size.Width = command.Model.Width;
        size.ShowInFilters = command.Model.ShowInFilters;
        historyWriter.Add(
            CatalogHistoryActionType.Update,
            CatalogHistoryEntityType.SheetSize,
            size.Id,
            $"Размер плиты #{size.Id} изменён: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateMaterialSheetSizeCommandResult(true);
    }
}