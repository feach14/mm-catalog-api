using System.Collections.Concurrent;
using Catalog.Api.Features.Edges.ForAdmin.Stroydvor.Dto;
using Catalog.Api.Features.Edges.Helpers;
using Catalog.Api.Services.History;
using Catalog.Api.Services.Stroydvor;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin.Stroydvor;

public sealed record SyncEdgesStroydvorCommand(
    [property: Description("Id кромок")] int[] Ids) : ICommand<SyncEdgesStroydvorCommandResult>;

public sealed record SyncEdgesStroydvorCommandResult(
    [property: Description("Результаты синхронизации")] EdgeStroydvorSyncDto[] Items);

public sealed class SyncEdgesStroydvorCommandValidator : AbstractValidator<SyncEdgesStroydvorCommand>
{
    public SyncEdgesStroydvorCommandValidator()
    {
        RuleFor(x => x.Ids)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указаны кромки для синхронизации")
            .Must(ids => ids.Length <= 100).WithMessage("За один запрос можно синхронизировать не более 100 кромок")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Id кромок не должны повторяться");
        RuleForEach(x => x.Ids).GreaterThan(0).WithMessage("Id кромки должен быть больше 0");
    }
}

public sealed class SyncEdgesStroydvorCommandHandler(
    CatalogDbContext dbContext,
    StroydvorProductClient productClient,
    ICatalogHistoryWriter historyWriter,
    TimeProvider timeProvider,
    ILogger<SyncEdgesStroydvorCommandHandler> logger) : ICommandHandler<SyncEdgesStroydvorCommand, SyncEdgesStroydvorCommandResult>
{
    public async Task<SyncEdgesStroydvorCommandResult> Handle(SyncEdgesStroydvorCommand command, CancellationToken ct)
    {
        var edges = await dbContext.Edges.AsNoTracking()
            .Include(x => x.SaleUnit)
            .Include(x => x.Size)
            .Include(x => x.Supplier)
            .Where(x => command.Ids.Contains(x.Id))
            .ToArrayAsync(ct);
        if (edges.Length != command.Ids.Length)
        {
            logger.LogError("Не удалось синхронизировать кромки со Стройдвором: одна из указанных кромок не найдена. Id кромок: {EdgeIds}",
                string.Join(", ", command.Ids));
            throw new BadHttpRequestException("Одна из указанных кромок не найдена.");
        }
        foreach (var edge in edges)
        {
            if (edge.StroydvorProductId is not > 0)
            {
                var edgeName = EdgeNameHelper.GetFullName(edge.Name, edge.Size.Name, edge.IsStockItem,
                    edge.Supplier.Name, edge.Supplier.IncludeInEdgeName);
                var error = $"У кромки #{edge.Id} «{edgeName}» нет привязки к Стройдвору.";
                LogSyncFailure(edge, error);
                throw new BadHttpRequestException(error);
            }
        }

        var reads = new ConcurrentDictionary<int, StroydvorEdgeProductReadResult>();
        await Parallel.ForEachAsync(edges, new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct }, async (edge, token) =>
        {
            var result = await productClient.ReadEdge(edge.StroydvorProductId!.Value, token);
            reads[edge.Id] = NormalizePrice(edge, result);
        });

        var byId = edges.ToDictionary(x => x.Id);
        var items = new List<EdgeStroydvorSyncDto>();
        // Network calls finish before row updates. History and snapshot changes commit together.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        foreach (var id in command.Ids)
        {
            var edge = byId[id];
            var edgeName = EdgeNameHelper.GetFullName(edge.Name, edge.Size.Name, edge.IsStockItem,
                edge.Supplier.Name, edge.Supplier.IncludeInEdgeName);
            var read = reads[id];
            if (!read.Success)
            {
                // A failed supplier read cannot confirm a change in price or availability.
                var current = await dbContext.Edges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
                var error = $"Кромка #{id} «{edgeName}»: {read.Error}";
                items.Add(CurrentResult(id, current, error));
                continue;
            }

            var product = read.Product!;
            var price = product.Price;
            var syncedAt = (DateTimeOffset?)timeProvider.GetUtcNow();
            var inStock = product.InStock;
            var article = product.Article ?? edge.StroydvorArticle;
            var priceUnit = edge.SaleUnit.SaleUnitType == EdgeSaleUnitTypeEnum.Roll ? "бухту" : "метр";
            // A binding or identity edited during the supplier request must not receive the old product's price.
            var changed = await dbContext.Edges
                .Where(x => x.Id == id && x.StroydvorProductId == edge.StroydvorProductId
                                      && x.ManufacturerId == edge.ManufacturerId
                                      && x.SaleUnitId == edge.SaleUnitId
                                      && x.SaleUnit.SaleUnitType == edge.SaleUnit.SaleUnitType
                                      && x.SaleUnit.Length == edge.SaleUnit.Length
                                      && x.Name == edge.Name && x.Article == edge.Article
                                      && x.StroydvorArticle == edge.StroydvorArticle
                                      && x.StroydvorPrice == edge.StroydvorPrice
                                      && x.StroydvorLastSyncAt == edge.StroydvorLastSyncAt
                                      && x.StroydvorInStock == edge.StroydvorInStock)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.StroydvorArticle, article)
                    .SetProperty(x => x.StroydvorPrice, price)
                    .SetProperty(x => x.StroydvorLastSyncAt, syncedAt)
                    .SetProperty(x => x.StroydvorInStock, inStock), ct);
            if (changed == 0)
            {
                var current = await dbContext.Edges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
                var error = $"Кромка #{id} «{edgeName}», привязка или данные синхронизации изменились во время запроса. Повторите синхронизацию.";
                LogSyncFailure(edge, error, read.Product);
                items.Add(CurrentResult(id, current, error));
                continue;
            }

            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, id,
                $"Кромка #{id} «{edgeName}»: синхронизирован товар Стройдвора #{edge.StroydvorProductId}, артикул «{article ?? "нет"}», цена {price} руб. за {priceUnit}, наличие: {(inStock ? "есть" : "нет")}.");
            items.Add(new EdgeStroydvorSyncDto(id, true, null,
                edge.StroydvorProductId, article, price, syncedAt, inStock,
                StroydvorProductClient.ProductUrl(edge.StroydvorProductId!.Value)));
        }
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SyncEdgesStroydvorCommandResult(items.ToArray());
    }

    private StroydvorEdgeProductReadResult NormalizePrice(Edge edge, StroydvorEdgeProductReadResult read)
    {
        var product = read.Product;
        var error = read.Error;
        if (product is not null)
        {
            if (product.SaleUnitType == EdgeSaleUnitTypeEnum.Roll && product.RollLengthM is not > 0)
                error = "Длина бухты на карточке Стройдвора не указана. Невозможно сопоставить цену с нашей единицей продажи.";
            else if (product.SaleUnitType == EdgeSaleUnitTypeEnum.Roll && product.RollLengthM < 1
                     && product.Price > decimal.MaxValue * product.RollLengthM.Value)
                error = "Цена Стройдвора после пересчёта в нашу единицу продажи превышает допустимую стоимость.";
            else
            {
                var pricePerMeter = product.SaleUnitType == EdgeSaleUnitTypeEnum.Roll
                    ? product.Price / product.RollLengthM!.Value : product.Price;
                var quantity = edge.SaleUnit.SaleUnitType == EdgeSaleUnitTypeEnum.Roll ? edge.SaleUnit.Length : 1m;
                const decimal maxPrice = 9999999999999999.99m;
                if (quantity <= 0 || pricePerMeter > maxPrice / quantity)
                    error = "Цена Стройдвора после пересчёта в нашу единицу продажи превышает допустимую стоимость.";
                else
                    return read with { Product = product with { Price = decimal.Round(pricePerMeter * quantity, 2, MidpointRounding.AwayFromZero) } };
            }
        }

        LogSyncFailure(edge, read.LogDetails is null ? error : $"{error} {read.LogDetails}", product);
        return product is null ? read : new StroydvorEdgeProductReadResult(null, error);
    }

    private void LogSyncFailure(Edge edge, string? error, StroydvorEdgeProductData? product = null) =>
        logger.LogError(
            "Не удалось синхронизировать кромку #{EdgeId} «{EdgeName}», артикул «{EdgeArticle}», с товаром Стройдвора #{StroydvorProductId}. Причина: {SyncError} " +
            "Единица продажи: у нас {SaleUnitType}, в Стройдворе {StroydvorSaleUnitType}. Длина бухты: у нас {SaleUnitLength} м, в Стройдворе {StroydvorRollLength} м. " +
            "Карточка Стройдвора «{StroydvorProductName}», артикул «{StroydvorArticle}».",
            edge.Id, EdgeNameHelper.GetFullName(edge.Name, edge.Size.Name, edge.IsStockItem,
                edge.Supplier.Name, edge.Supplier.IncludeInEdgeName), edge.Article, edge.StroydvorProductId, error,
            edge.SaleUnit.SaleUnitType, product?.SaleUnitType, edge.SaleUnit.Length, product?.RollLengthM,
            product?.Name, product?.Article);

    private static EdgeStroydvorSyncDto CurrentResult(int id, Edge? edge, string error) =>
        new(id, false, error, edge?.StroydvorProductId, edge?.StroydvorArticle,
            edge?.StroydvorPrice ?? 0, edge?.StroydvorLastSyncAt, edge?.StroydvorInStock ?? false,
            edge?.StroydvorProductId is > 0 ? StroydvorProductClient.ProductUrl(edge.StroydvorProductId.Value) : null);
}