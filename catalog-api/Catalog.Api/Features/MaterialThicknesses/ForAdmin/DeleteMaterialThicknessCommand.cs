using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin;

public sealed record DeleteMaterialThicknessCommand(int Id) : ICommand<DeleteMaterialThicknessCommandResult>;
public sealed record DeleteMaterialThicknessCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteMaterialThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteMaterialThicknessCommand, DeleteMaterialThicknessCommandResult>
{
    public async Task<DeleteMaterialThicknessCommandResult> Handle(DeleteMaterialThicknessCommand command, CancellationToken ct)
    {
        var thickness = await dbContext.MaterialThicknesses.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
                        ?? throw new BadHttpRequestException($"Толщина материала с id={command.Id} не найдена.");
        if (await dbContext.Materials.AsNoTracking().AnyAsync(x => x.MaterialThicknessId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить толщину, используемую материалами.");

        dbContext.MaterialThicknesses.Remove(thickness);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.Thickness,
            thickness.Id,
            $"Удалена толщина материала #{thickness.Id} «{thickness.Name}»: {thickness.Value} мм.");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteMaterialThicknessCommandResult(true);
    }
}