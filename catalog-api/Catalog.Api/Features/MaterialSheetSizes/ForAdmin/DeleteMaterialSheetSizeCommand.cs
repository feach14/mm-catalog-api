using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record DeleteMaterialSheetSizeCommand(int Id) : ICommand<DeleteMaterialSheetSizeCommandResult>;

public sealed record DeleteMaterialSheetSizeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteMaterialSheetSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteMaterialSheetSizeCommand, DeleteMaterialSheetSizeCommandResult>
{
    public async Task<DeleteMaterialSheetSizeCommandResult> Handle(DeleteMaterialSheetSizeCommand command, CancellationToken ct)
    {
        var size = await dbContext.MaterialSheetSizes.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
                   ?? throw new BadHttpRequestException($"Размер материала с id={command.Id} не найден.");

        if (await dbContext.Materials.AnyAsync(x => x.MaterialSheetSizeId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить размер, используемый материалами.");

        dbContext.MaterialSheetSizes.Remove(size);

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.SheetSize,
            size.Id,
            $"Удалён размер плиты #{size.Id} «{size.Name}». Перед удалением: {size.Height}×{size.Width}, показывать в фильтрах — {(size.ShowInFilters ? "Да" : "Нет")}.");

        await dbContext.SaveChangesAsync(ct);
        return new DeleteMaterialSheetSizeCommandResult(true);
    }
}