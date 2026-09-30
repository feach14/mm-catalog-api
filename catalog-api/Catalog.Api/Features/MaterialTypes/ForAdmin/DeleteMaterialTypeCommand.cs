using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record DeleteMaterialTypeCommand(int Id) : ICommand<DeleteMaterialTypeCommandResult>;

public sealed record DeleteMaterialTypeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteMaterialTypeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteMaterialTypeCommand, DeleteMaterialTypeCommandResult>
{
    public async Task<DeleteMaterialTypeCommandResult> Handle(DeleteMaterialTypeCommand command, CancellationToken ct)
    {
        var materialType = await dbContext.MaterialTypes.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Тип материала с id={command.Id} не найден.");

        if (await dbContext.Materials.AnyAsync(x => x.MaterialTypeId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить тип материала, который используется материалами.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        dbContext.MaterialTypes.Remove(materialType);

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.MaterialType,
            materialType.Id,
            $"Удалён тип материала #{materialType.Id} «{materialType.Name}».");

        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new DeleteMaterialTypeCommandResult(true);
    }
}