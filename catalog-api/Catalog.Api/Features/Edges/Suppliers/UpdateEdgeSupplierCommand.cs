using Catalog.Api.Features.Edges.Suppliers.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record UpdateEdgeSupplierCommand(int Id, EdgeSupplierModel Model) : ICommand<UpdateEdgeSupplierCommandResult>;

public sealed record UpdateEdgeSupplierCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeSupplierCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeSupplierCommand, UpdateEdgeSupplierCommandResult>
{
    public async Task<UpdateEdgeSupplierCommandResult> Handle(UpdateEdgeSupplierCommand command, CancellationToken ct)
    {
        EdgeSupplier supplier = await dbContext.EdgeSuppliers.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Поставщик кромок не найден.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (supplier.Name != name)
            changes.Add($"название «{supplier.Name}» → «{name}»");
        if (supplier.IncludeInEdgeName != command.Model.IncludeInEdgeName)
            changes.Add($"включать в название кромки: {(supplier.IncludeInEdgeName ? "да" : "нет")} → {(command.Model.IncludeInEdgeName ? "да" : "нет")}");

        if (changes.Count == 0)
            return new UpdateEdgeSupplierCommandResult(true);

        supplier.Name = name;
        supplier.IncludeInEdgeName = command.Model.IncludeInEdgeName;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.EdgeSupplier,
            supplier.Id,
            $"Поставщик кромок #{supplier.Id} «{supplier.Name}»: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateEdgeSupplierCommandResult(true);
    }
}