using Catalog.Api.Features.Manufacturers.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record CreateManufacturerCommand(MaterialManufacturerModel Manufacturer) : ICommand<CreateManufacturerCommandResult>;

public sealed record CreateManufacturerCommandResult(
    [property: Description("Id производителя")] int Id);

public sealed class CreateManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateManufacturerCommand, CreateManufacturerCommandResult>
{
    public async Task<CreateManufacturerCommandResult> Handle(CreateManufacturerCommand command, CancellationToken ct)
    {
        var maxOrderByCol = await dbContext.MaterialManufacturers.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
        var manufacturer = new MaterialManufacturer
        {
            Name = command.Manufacturer.Name.Trim(),
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.MaterialManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync(ct);

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.Manufacturer,
            manufacturer.Id,
            $"Создан производитель #{manufacturer.Id} «{manufacturer.Name}».");
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new CreateManufacturerCommandResult(manufacturer.Id);
    }
}