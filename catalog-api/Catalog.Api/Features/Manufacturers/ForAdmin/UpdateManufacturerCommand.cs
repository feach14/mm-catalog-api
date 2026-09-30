using Catalog.Api.Features.Manufacturers.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record UpdateManufacturerCommand(int Id, MaterialManufacturerModel Manufacturer) : ICommand<UpdateManufacturerCommandResult>;

public sealed record UpdateManufacturerCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateManufacturerCommand, UpdateManufacturerCommandResult>
{
    public async Task<UpdateManufacturerCommandResult> Handle(UpdateManufacturerCommand command, CancellationToken ct)
    {
        var manufacturer = await dbContext.MaterialManufacturers.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");

        var newName = command.Manufacturer.Name.Trim();
        if (manufacturer.Name == newName)
            return new UpdateManufacturerCommandResult(true);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var oldName = manufacturer.Name;
        manufacturer.Name = newName;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.Manufacturer,
            manufacturer.Id,
            $"Производитель #{manufacturer.Id} изменён: название «{oldName}» → «{newName}».");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new UpdateManufacturerCommandResult(true);
    }
}