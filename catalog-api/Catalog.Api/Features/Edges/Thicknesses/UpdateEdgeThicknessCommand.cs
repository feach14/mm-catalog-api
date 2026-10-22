using Catalog.Api.Features.Edges.Thicknesses.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Thicknesses;

public sealed record UpdateEdgeThicknessCommand(int Id, EdgeThicknessModel Model) : ICommand<UpdateEdgeThicknessCommandResult>;

public sealed record UpdateEdgeThicknessCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeThicknessCommand, UpdateEdgeThicknessCommandResult>
{
    public async Task<UpdateEdgeThicknessCommandResult> Handle(UpdateEdgeThicknessCommand command, CancellationToken ct)
    {
        EdgeThickness thickness = await dbContext.EdgeThicknesses.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись толщины кромок не найдена.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (thickness.Name != name)
            changes.Add($"название «{thickness.Name}» → «{name}»");
        if (thickness.Value != command.Model.Value)
            changes.Add($"толщина {thickness.Value} → {command.Model.Value} мм");

        if (changes.Count == 0)
            return new UpdateEdgeThicknessCommandResult(true);

        thickness.Name = name;
        thickness.Value = command.Model.Value;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.EdgeThickness,
            thickness.Id,
            $"Толщина кромок #{thickness.Id}: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateEdgeThicknessCommandResult(true);
    }
}