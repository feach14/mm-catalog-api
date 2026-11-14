using Catalog.Api.Features.Edges.Sizes.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record CreateEdgeSizeCommand(EdgeSizeModel Model) : ICommand<CreateEdgeSizeCommandResult>;

public sealed record CreateEdgeSizeCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateEdgeSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeSizeCommand, CreateEdgeSizeCommandResult>
{
    public async Task<CreateEdgeSizeCommandResult> Handle(CreateEdgeSizeCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var maxOrderByCol = await dbContext.EdgeSizes.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
            if (maxOrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка размеров кромок.");
            EdgeSize size = new()
            {
                Name = command.Model.Name.Trim(),
                Thickness = command.Model.Thickness,
                Width = command.Model.Width,
                OrderByCol = maxOrderByCol + 1
            };

            dbContext.EdgeSizes.Add(size);
            await dbContext.SaveChangesAsync(ct);
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Create,
                CatalogHistoryEntityTypeEnum.EdgeSize,
                size.Id,
                $"Создан размер кромок #{size.Id} «{size.Name}»: {size.Thickness} мм, ширина {size.Width} мм.");
            await dbContext.SaveChangesAsync(ct);
            return new CreateEdgeSizeCommandResult(size.Id);
        }, ct);
    }
}