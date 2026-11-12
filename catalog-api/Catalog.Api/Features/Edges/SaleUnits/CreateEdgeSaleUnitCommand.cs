using Catalog.Api.Features.Edges.SaleUnits.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record CreateEdgeSaleUnitCommand(EdgeSaleUnitModel Model) : ICommand<CreateEdgeSaleUnitCommandResult>;

public sealed record CreateEdgeSaleUnitCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateEdgeSaleUnitCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeSaleUnitCommand, CreateEdgeSaleUnitCommandResult>
{
    public async Task<CreateEdgeSaleUnitCommandResult> Handle(CreateEdgeSaleUnitCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var maxOrderByCol = await dbContext.EdgeSaleUnits.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
            if (maxOrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка единиц продажи кромок.");
            EdgeSaleUnit saleUnit = new()
            {
                Name = command.Model.Name.Trim(),
                SaleUnitType = command.Model.SaleUnitType,
                Length = command.Model.Length,
                OrderByCol = maxOrderByCol + 1
            };

            dbContext.EdgeSaleUnits.Add(saleUnit);
            await dbContext.SaveChangesAsync(ct);
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Create,
                CatalogHistoryEntityTypeEnum.EdgeSaleUnit,
                saleUnit.Id,
                $"Создана единица продажи кромок #{saleUnit.Id} «{saleUnit.Name}»: {saleUnit.Length} м.");
            await dbContext.SaveChangesAsync(ct);
            return new CreateEdgeSaleUnitCommandResult(saleUnit.Id);
        }, ct);
    }
}