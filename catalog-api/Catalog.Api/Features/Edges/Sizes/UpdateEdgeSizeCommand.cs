using Catalog.Api.Features.Edges.Sizes.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record UpdateEdgeSizeCommand(int Id, EdgeSizeModel Model) : ICommand<UpdateEdgeSizeCommandResult>;

public sealed record UpdateEdgeSizeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateEdgeSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeSizeCommand, UpdateEdgeSizeCommandResult>
{
    public async Task<UpdateEdgeSizeCommandResult> Handle(UpdateEdgeSizeCommand command, CancellationToken ct)
    {
        EdgeSize size = await dbContext.EdgeSizes.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись размера кромок не найдена.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (size.Name != name)
            changes.Add($"название «{size.Name}» → «{name}»");
        if (size.Thickness != command.Model.Thickness)
            changes.Add($"толщина {size.Thickness} → {command.Model.Thickness} мм");
        if (size.Width != command.Model.Width)
            changes.Add($"ширина {size.Width} → {command.Model.Width} мм");

        if (changes.Count == 0)
            return new UpdateEdgeSizeCommandResult(true);

        size.Name = name;
        size.Thickness = command.Model.Thickness;
        size.Width = command.Model.Width;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.EdgeSize,
            size.Id,
            $"Размер кромок #{size.Id}: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateEdgeSizeCommandResult(true);
    }
}