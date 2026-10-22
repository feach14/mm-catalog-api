using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Manufacturers;

public sealed record DeleteEdgeManufacturerCommand(int Id) : ICommand<DeleteEdgeManufacturerCommandResult>;

public sealed record DeleteEdgeManufacturerCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeManufacturerCommand, DeleteEdgeManufacturerCommandResult>
{
    public async Task<DeleteEdgeManufacturerCommandResult> Handle(DeleteEdgeManufacturerCommand command, CancellationToken ct)
    {
        EdgeManufacturer manufacturer = await dbContext.EdgeManufacturers.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись производителя кромок не найдена.");
        if (await dbContext.Edges.AsNoTracking().AnyAsync(x => x.ManufacturerId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить запись производителя кромок, которая используется кромками.");

        dbContext.EdgeManufacturers.Remove(manufacturer);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.EdgeManufacturer,
            manufacturer.Id,
            $"Удалён производитель кромок #{manufacturer.Id} «{manufacturer.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeManufacturerCommandResult(true);
    }
}