using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record DeleteEdgeCommand(int Id) : ICommand<DeleteEdgeCommandResult>;

public sealed record DeleteEdgeCommandResult([property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeCommand, DeleteEdgeCommandResult>
{
    public async Task<DeleteEdgeCommandResult> Handle(DeleteEdgeCommand command, CancellationToken ct)
    {
        var edge = await dbContext.Edges.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Кромка с id={command.Id} не найдена.");
        var imageGuids = edge.Images.Select(x => x.Guid).ToArray();
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete, CatalogHistoryEntityTypeEnum.Edge, edge.Id,
            $"Удалена кромка #{edge.Id} «{edge.Name}»: артикул {edge.Article}, производитель #{edge.ManufacturerId}, толщина #{edge.ThicknessId}, единица продажи #{edge.SaleUnitId}, складская позиция: {edge.IsStockItem}, остаток {edge.StockQuantity?.ToString() ?? "нет"} м, изображения {string.Join(", ", imageGuids)}.");
        dbContext.Edges.Remove(edge);
        await dbContext.SaveChangesAsync(ct);
        foreach (var guid in imageGuids) memoryCache.Remove(("edge-image", guid));
        return new DeleteEdgeCommandResult(true);
    }
}