using Catalog.Api.Features.Edges.Manufacturers.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Manufacturers;

public sealed record UpdateEdgeManufacturerCommand(int Id, EdgeManufacturerModel Model) : ICommand<UpdateEdgeManufacturerCommandResult>;

public sealed record UpdateEdgeManufacturerCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeManufacturerCommand, UpdateEdgeManufacturerCommandResult>
{
    public async Task<UpdateEdgeManufacturerCommandResult> Handle(UpdateEdgeManufacturerCommand command, CancellationToken ct)
    {
        EdgeManufacturer manufacturer = await dbContext.EdgeManufacturers.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись производителя кромок не найдена.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (manufacturer.Name != name)
            changes.Add($"название «{manufacturer.Name}» → «{name}»");

        if (changes.Count == 0)
            return new UpdateEdgeManufacturerCommandResult(true);

        manufacturer.Name = name;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.EdgeManufacturer,
            manufacturer.Id,
            $"Производитель кромок #{manufacturer.Id}: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateEdgeManufacturerCommandResult(true);
    }
}