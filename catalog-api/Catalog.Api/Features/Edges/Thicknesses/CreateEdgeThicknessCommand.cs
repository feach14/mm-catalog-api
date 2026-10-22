using Catalog.Api.Features.Edges.Thicknesses.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Thicknesses;

public sealed record CreateEdgeThicknessCommand(EdgeThicknessModel Model) : ICommand<CreateEdgeThicknessCommandResult>;

public sealed record CreateEdgeThicknessCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateEdgeThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeThicknessCommand, CreateEdgeThicknessCommandResult>
{
    public async Task<CreateEdgeThicknessCommandResult> Handle(CreateEdgeThicknessCommand command, CancellationToken ct)
    {
        EdgeThickness thickness = new()
        {
            Name = command.Model.Name.Trim(),
            Value = command.Model.Value
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.EdgeThicknesses.Add(thickness);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.EdgeThickness,
            thickness.Id,
            $"Создана толщина кромок #{thickness.Id} «{thickness.Name}»: {thickness.Value} мм.");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CreateEdgeThicknessCommandResult(thickness.Id);
    }
}