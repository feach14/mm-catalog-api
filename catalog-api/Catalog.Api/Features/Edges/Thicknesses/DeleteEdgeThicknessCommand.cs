using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Thicknesses;

public sealed record DeleteEdgeThicknessCommand(int Id) : ICommand<DeleteEdgeThicknessCommandResult>;

public sealed record DeleteEdgeThicknessCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeThicknessCommand, DeleteEdgeThicknessCommandResult>
{
    public async Task<DeleteEdgeThicknessCommandResult> Handle(DeleteEdgeThicknessCommand command, CancellationToken ct)
    {
        EdgeThickness thickness = await dbContext.EdgeThicknesses.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись толщины кромок не найдена.");
        if (await dbContext.Edges.AsNoTracking().AnyAsync(x => x.ThicknessId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить запись толщины кромок, которая используется кромками.");

        dbContext.EdgeThicknesses.Remove(thickness);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.EdgeThickness,
            thickness.Id,
            $"Удалена толщина кромок #{thickness.Id} «{thickness.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeThicknessCommandResult(true);
    }
}