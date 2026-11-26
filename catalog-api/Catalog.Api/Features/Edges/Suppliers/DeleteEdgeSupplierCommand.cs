using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record DeleteEdgeSupplierCommand(int Id) : ICommand<DeleteEdgeSupplierCommandResult>;

public sealed record DeleteEdgeSupplierCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeSupplierCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeSupplierCommand, DeleteEdgeSupplierCommandResult>
{
    public async Task<DeleteEdgeSupplierCommandResult> Handle(DeleteEdgeSupplierCommand command, CancellationToken ct)
    {
        EdgeSupplier supplier = await dbContext.EdgeSuppliers.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Поставщик кромок не найден.");
        if (await dbContext.Edges.AsNoTracking().AnyAsync(x => x.SupplierId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить поставщика, который используется кромками.");

        dbContext.EdgeSuppliers.Remove(supplier);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.EdgeSupplier,
            supplier.Id,
            $"Удалён поставщик кромок #{supplier.Id} «{supplier.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeSupplierCommandResult(true);
    }
}