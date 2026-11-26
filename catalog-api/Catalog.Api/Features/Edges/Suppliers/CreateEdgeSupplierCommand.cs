using Catalog.Api.Features.Edges.Suppliers.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record CreateEdgeSupplierCommand(EdgeSupplierModel Model) : ICommand<CreateEdgeSupplierCommandResult>;

public sealed record CreateEdgeSupplierCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateEdgeSupplierCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeSupplierCommand, CreateEdgeSupplierCommandResult>
{
    public async Task<CreateEdgeSupplierCommandResult> Handle(CreateEdgeSupplierCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var maxOrderByCol = await dbContext.EdgeSuppliers.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
            if (maxOrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка поставщиков кромок.");
            EdgeSupplier supplier = new()
            {
                Name = command.Model.Name.Trim(),
                IncludeInEdgeName = command.Model.IncludeInEdgeName,
                OrderByCol = maxOrderByCol + 1
            };

            dbContext.EdgeSuppliers.Add(supplier);
            await dbContext.SaveChangesAsync(ct);
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Create,
                CatalogHistoryEntityTypeEnum.EdgeSupplier,
                supplier.Id,
                $"Создан поставщик кромок #{supplier.Id} «{supplier.Name}»: включать в название кромки — {(supplier.IncludeInEdgeName ? "да" : "нет")}.");
            await dbContext.SaveChangesAsync(ct);
            return new CreateEdgeSupplierCommandResult(supplier.Id);
        }, ct);
    }
}