using Catalog.Api.Features.MaterialTypes.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record CreateMaterialTypeCommand(MaterialTypeModel Model) : ICommand<CreateMaterialTypeCommandResult>;

public sealed record CreateMaterialTypeCommandResult(
    [property: Description("Id типа материала")] int Id);

public sealed class CreateMaterialTypeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateMaterialTypeCommand, CreateMaterialTypeCommandResult>
{
    public async Task<CreateMaterialTypeCommandResult> Handle(CreateMaterialTypeCommand command, CancellationToken ct)
    {
        var maxOrderByCol = await dbContext.MaterialTypes.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;

        var materialType = new MaterialType
        {
            Name = command.Model.Name.Trim(),
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        dbContext.MaterialTypes.Add(materialType);
        await dbContext.SaveChangesAsync(ct);

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.MaterialType,
            materialType.Id,
            $"Создан тип материала #{materialType.Id} «{materialType.Name}».");
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new CreateMaterialTypeCommandResult(materialType.Id);
    }
}