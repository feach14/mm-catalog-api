using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record DeleteManufacturerCommand(int Id) : ICommand<DeleteManufacturerCommandResult>;

public sealed record DeleteManufacturerCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteManufacturerCommand, DeleteManufacturerCommandResult>
{
    public async Task<DeleteManufacturerCommandResult> Handle(DeleteManufacturerCommand command, CancellationToken ct)
    {
        var manufacturer = await dbContext.MaterialManufacturers.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");
        if (await dbContext.Materials.AnyAsync(x => x.MaterialManufacturerId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить производителя, который используется материалами.");

        dbContext.MaterialManufacturers.Remove(manufacturer);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.Manufacturer,
            manufacturer.Id,
            $"Удалён производитель #{manufacturer.Id} «{manufacturer.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteManufacturerCommandResult(true);
    }
}