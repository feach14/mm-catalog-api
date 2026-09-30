using Catalog.Api.Features.MaterialSheetSizes.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record CreateMaterialSheetSizeCommand(MaterialSheetSizeModel Model) : ICommand<CreateMaterialSheetSizeCommandResult>;

public sealed record CreateMaterialSheetSizeCommandResult(
    [property: Description("Id размера материала")] int Id);

public sealed class CreateMaterialSheetSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
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
            ShowInFilters = command.Model.ShowInFilters,
            OrderByCol = maxOrderByCol + 1
        };
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.MaterialSheetSizes.Add(size);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.SheetSize,
            size.Id,
            $"Создан размер плиты #{size.Id} «{size.Name}»: {size.Height}×{size.Width}, показывать в фильтрах — {(size.ShowInFilters ? "Да" : "Нет")}.");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CreateMaterialSheetSizeCommandResult(size.Id);
    }
}