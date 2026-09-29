using Catalog.Api.Features.MaterialThicknesses.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin;

public sealed record CreateMaterialThicknessCommand(MaterialThicknessModel Model) : ICommand<CreateMaterialThicknessCommandResult>;
public sealed record CreateMaterialThicknessCommandResult(
    [property: Description("Id толщины материала")] int Id);

public sealed class CreateMaterialThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateMaterialThicknessCommand, CreateMaterialThicknessCommandResult>
{
    public async Task<CreateMaterialThicknessCommandResult> Handle(CreateMaterialThicknessCommand command, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var thickness = new MaterialThickness { Name = command.Model.Name.Trim(), Value = command.Model.Value };
        dbContext.MaterialThicknesses.Add(thickness);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.Thickness,
            thickness.Id,
            $"Создана толщина материала #{thickness.Id} «{thickness.Name}»: {thickness.Value} мм.");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CreateMaterialThicknessCommandResult(thickness.Id);
    }
}