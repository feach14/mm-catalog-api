using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record DeleteEdgeSaleUnitCommand(int Id) : ICommand<DeleteEdgeSaleUnitCommandResult>;

public sealed record DeleteEdgeSaleUnitCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeSaleUnitCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeSaleUnitCommand, DeleteEdgeSaleUnitCommandResult>
{
    public async Task<DeleteEdgeSaleUnitCommandResult> Handle(DeleteEdgeSaleUnitCommand command, CancellationToken ct)
    {
        EdgeSaleUnit saleUnit = await dbContext.EdgeSaleUnits.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись единицы продажи кромок не найдена.");
        if (await dbContext.Edges.AsNoTracking().AnyAsync(x => x.SaleUnitId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить запись единицы продажи кромок, которая используется кромками.");

        dbContext.EdgeSaleUnits.Remove(saleUnit);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.EdgeSaleUnit,
            saleUnit.Id,
            $"Удалена единица продажи кромок #{saleUnit.Id} «{saleUnit.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeSaleUnitCommandResult(true);
    }
}