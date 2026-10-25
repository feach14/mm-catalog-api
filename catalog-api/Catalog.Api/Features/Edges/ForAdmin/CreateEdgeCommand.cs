using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record CreateEdgeCommand(CreateEdgeModel Model) : ICommand<CreateEdgeCommandResult>;

public sealed record CreateEdgeModel : EdgeModel;

public sealed class CreateEdgeModelValidator : AbstractValidator<CreateEdgeModel>
{
    public CreateEdgeModelValidator(CatalogDbContext dbContext)
    {
        Include(new EdgeModelValidator(dbContext));
        RuleFor(x => x.Image)
            .MustAsync(async (guid, ct) => await dbContext.EdgeImageCache.AsNoTracking()
                .AnyAsync(x => x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Original, ct))
            .When(x => x.Image is not null).WithMessage("Оригинальное изображение не найдено среди загруженных файлов кромок");
        RuleFor(x => x.Thumbnail240)
            .MustAsync(async (guid, ct) => await dbContext.EdgeImageCache.AsNoTracking()
                .AnyAsync(x => x.Guid == guid && x.ImageType == EdgeImageTypeEnum.Thumbnail240, ct))
            .When(x => x.Thumbnail240 is not null).WithMessage("Миниатюра не найдена среди загруженных файлов кромок");
    }
}

public sealed record CreateEdgeCommandResult([property: Description("Id")] int Id);

public sealed class CreateEdgeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateEdgeCommand, CreateEdgeCommandResult>
{
    public async Task<CreateEdgeCommandResult> Handle(CreateEdgeCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var model = command.Model;
            var maxOrderByCol = await dbContext.Edges.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
            if (maxOrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка кромок. Упорядочьте существующие записи перед добавлением.");
            var edge = new Edge
            {
                Name = model.Name.Trim(),
                Article = model.Article.Trim(),
                ManufacturerId = model.ManufacturerId,
                ThicknessId = model.ThicknessId,
                SaleUnitId = model.SaleUnitId,
                IsStockItem = model.IsStockItem,
                StockQuantity = model.StockQuantity,
                OrderByCol = maxOrderByCol + 1
            };
            dbContext.Edges.Add(edge);
            await dbContext.SaveChangesAsync(ct);

            Guid?[] imageGuids = [model.Image, model.Thumbnail240];
            foreach (var guid in imageGuids.Where(x => x.HasValue))
            {
                var cached = await dbContext.EdgeImageCache.SingleAsync(x => x.Guid == guid, ct);
                dbContext.EdgeImages.Add(new EdgeImage
                {
                    EdgeId = edge.Id,
                    Guid = cached.Guid,
                    Type = cached.Type,
                    Data = cached.Data,
                    ImageType = cached.ImageType
                });
                dbContext.EdgeImageCache.Remove(cached);
            }
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Create, CatalogHistoryEntityTypeEnum.Edge, edge.Id,
                $"Создана кромка #{edge.Id} «{edge.Name}»: артикул {edge.Article}, производитель #{edge.ManufacturerId}, толщина #{edge.ThicknessId}, единица продажи #{edge.SaleUnitId}, складская позиция: {edge.IsStockItem}, остаток {edge.StockQuantity?.ToString() ?? "нет"} м, оригинал {model.Image?.ToString() ?? "нет"}, миниатюра {model.Thumbnail240?.ToString() ?? "нет"}.");
            await dbContext.SaveChangesAsync(ct);
            return new CreateEdgeCommandResult(edge.Id);
        }, ct);
    }
}