using Catalog.Api.Features.MaterialTypes.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record UpdateMaterialTypeCommand(int Id, MaterialTypeModel Model) : ICommand<UpdateMaterialTypeCommandResult>;

public sealed record UpdateMaterialTypeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateMaterialTypeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateMaterialTypeCommand, UpdateMaterialTypeCommandResult>
{
    public async Task<UpdateMaterialTypeCommandResult> Handle(UpdateMaterialTypeCommand command, CancellationToken ct)
    {
        var materialType = await dbContext.MaterialTypes.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
                           ?? throw new BadHttpRequestException("Указанный тип материала не найден");

        var newName = command.Model.Name.Trim();
        var changes = new List<string>();
        if (materialType.Name != newName)
            changes.Add($"название «{materialType.Name}» → «{newName}»");
        if (materialType.HideOnSite != command.Model.HideOnSite)
            changes.Add($"скрыт на сайте {materialType.HideOnSite} → {command.Model.HideOnSite}");

        if (changes.Count == 0)
            return new UpdateMaterialTypeCommandResult(true);

        materialType.Name = newName;
        materialType.HideOnSite = command.Model.HideOnSite;

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.MaterialType,
            materialType.Id,
            $"Тип материала #{materialType.Id} изменён: {string.Join(", ", changes)}.");

        await dbContext.SaveChangesAsync(ct);

        return new UpdateMaterialTypeCommandResult(true);
    }
}