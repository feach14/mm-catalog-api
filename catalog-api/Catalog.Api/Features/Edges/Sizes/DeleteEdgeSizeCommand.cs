using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record DeleteEdgeSizeCommand(int Id) : ICommand<DeleteEdgeSizeCommandResult>;

public sealed record DeleteEdgeSizeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeSizeCommand, DeleteEdgeSizeCommandResult>
{
    public async Task<DeleteEdgeSizeCommandResult> Handle(DeleteEdgeSizeCommand command, CancellationToken ct)
    {
        EdgeSize size = await dbContext.EdgeSizes.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Запись размера кромок не найдена.");
        if (await dbContext.Edges.AsNoTracking().AnyAsync(x => x.SizeId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить запись размера кромок, которая используется кромками.");

        dbContext.EdgeSizes.Remove(size);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.EdgeSize,
            size.Id,
            $"Удалён размер кромок #{size.Id} «{size.Name}»: {size.Thickness} мм, ширина {size.Width} мм.");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeSizeCommandResult(true);
    }
}