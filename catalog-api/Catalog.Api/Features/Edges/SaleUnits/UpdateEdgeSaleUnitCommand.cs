using Catalog.Api.Features.Edges.SaleUnits.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record UpdateEdgeSaleUnitCommand(int Id, EdgeSaleUnitModel Model) : ICommand<UpdateEdgeSaleUnitCommandResult>;

public sealed record UpdateEdgeSaleUnitCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeSaleUnitCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeSaleUnitCommand, UpdateEdgeSaleUnitCommandResult>
{
    public async Task<UpdateEdgeSaleUnitCommandResult> Handle(UpdateEdgeSaleUnitCommand command, CancellationToken ct)
    {
        EdgeSaleUnit saleUnit = await dbContext.EdgeSaleUnits.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись единицы продажи кромок не найдена.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (saleUnit.Name != name)
            changes.Add($"название «{saleUnit.Name}» → «{name}»");
        if (saleUnit.Type != command.Model.Type)
        {
            string oldType = saleUnit.Type == EdgeSaleUnitTypeEnum.Roll ? "бухта" : "погонный метр";
            string newType = command.Model.Type == EdgeSaleUnitTypeEnum.Roll ? "бухта" : "погонный метр";
            changes.Add($"тип «{oldType}» → «{newType}»");
        }
        if (saleUnit.Length != command.Model.Length)
            changes.Add($"длина {saleUnit.Length} → {command.Model.Length} м");

        if (changes.Count == 0)
            return new UpdateEdgeSaleUnitCommandResult(true);

        saleUnit.Name = name;
        saleUnit.Type = command.Model.Type;
        saleUnit.Length = command.Model.Length;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.EdgeSaleUnit,
            saleUnit.Id,
            $"Единица продажи кромок #{saleUnit.Id}: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateEdgeSaleUnitCommandResult(true);
    }
}