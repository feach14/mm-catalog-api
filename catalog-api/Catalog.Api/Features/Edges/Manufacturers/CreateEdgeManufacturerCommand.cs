using Catalog.Api.Features.Edges.Manufacturers.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Manufacturers;

public sealed record CreateEdgeManufacturerCommand(EdgeManufacturerModel Model) : ICommand<CreateEdgeManufacturerCommandResult>;

public sealed record CreateEdgeManufacturerCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateEdgeManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeManufacturerCommand, CreateEdgeManufacturerCommandResult>
{
    public async Task<CreateEdgeManufacturerCommandResult> Handle(CreateEdgeManufacturerCommand command, CancellationToken ct)
    {
        EdgeManufacturer manufacturer = new()
        {
            Name = command.Model.Name.Trim()
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.EdgeManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.EdgeManufacturer,
            manufacturer.Id,
            $"Создан производитель кромок #{manufacturer.Id} «{manufacturer.Name}».");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CreateEdgeManufacturerCommandResult(manufacturer.Id);
    }
}