using Catalog.Api.Features.Edges.SaleUnits;
using Catalog.Api.Features.Edges.SaleUnits.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/sale-units"),
 OpenApiTagOrder(14)]
public sealed class EdgeSaleUnitsForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeSaleUnits)),
     EndpointDescription("Список единиц продажи кромок"),
     ProducesResponseType(typeof(GetEdgeSaleUnitsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список единиц продажи кромок")]
    public Task<GetEdgeSaleUnitsQueryResult> EdgeSaleUnits(
        [FromServices] IQueryHandler<GetEdgeSaleUnitsQuery, GetEdgeSaleUnitsQueryResult> handler) =>
        handler.Handle(new GetEdgeSaleUnitsQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(EdgeSaleUnit)),
     EndpointDescription("Единица продажи кромок"),
     ProducesResponseType(typeof(EdgeSaleUnitDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Единица продажи кромок")]
    public Task<EdgeSaleUnitDto> EdgeSaleUnit(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetEdgeSaleUnitQuery, EdgeSaleUnitDto> handler) =>
        handler.Handle(new GetEdgeSaleUnitQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdgeSaleUnit)),
     EndpointDescription("Создание единицы продажи кромок"),
     ProducesResponseType(typeof(CreateEdgeSaleUnitCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateEdgeSaleUnitCommandResult> CreateEdgeSaleUnit(
        [FromBody, Description("Параметры единицы продажи кромок")] EdgeSaleUnitModel model,
        [FromServices] ICommandHandler<CreateEdgeSaleUnitCommand, CreateEdgeSaleUnitCommandResult> handler) =>
        handler.Handle(new CreateEdgeSaleUnitCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdgeSaleUnit)),
     EndpointDescription("Обновление единицы продажи кромок"),
     ProducesResponseType(typeof(UpdateEdgeSaleUnitCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateEdgeSaleUnitCommandResult> UpdateEdgeSaleUnit(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры единицы продажи кромок")] EdgeSaleUnitModel model,
        [FromServices] ICommandHandler<UpdateEdgeSaleUnitCommand, UpdateEdgeSaleUnitCommandResult> handler) =>
        handler.Handle(new UpdateEdgeSaleUnitCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdgeSaleUnit)),
     EndpointDescription("Удаление единицы продажи кромок"),
     ProducesResponseType(typeof(DeleteEdgeSaleUnitCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeSaleUnitCommandResult> DeleteEdgeSaleUnit(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteEdgeSaleUnitCommand, DeleteEdgeSaleUnitCommandResult> handler) =>
        handler.Handle(new DeleteEdgeSaleUnitCommand(id), HttpContext.RequestAborted);
}