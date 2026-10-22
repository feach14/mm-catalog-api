using Catalog.Api.Features.Edges.Manufacturers;
using Catalog.Api.Features.Edges.Manufacturers.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/manufacturers"),
 OpenApiTagOrder(12)]
public sealed class EdgeManufacturersForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeManufacturers)),
     EndpointDescription("Список производителей кромок"),
     ProducesResponseType(typeof(GetEdgeManufacturersQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список производителей кромок")]
    public Task<GetEdgeManufacturersQueryResult> EdgeManufacturers(
        [FromServices] IQueryHandler<GetEdgeManufacturersQuery, GetEdgeManufacturersQueryResult> handler) =>
        handler.Handle(new GetEdgeManufacturersQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(EdgeManufacturer)),
     EndpointDescription("Производитель кромок"),
     ProducesResponseType(typeof(EdgeManufacturerDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Производитель кромок")]
    public Task<EdgeManufacturerDto> EdgeManufacturer(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetEdgeManufacturerQuery, EdgeManufacturerDto> handler) =>
        handler.Handle(new GetEdgeManufacturerQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdgeManufacturer)),
     EndpointDescription("Создание производителя кромок"),
     ProducesResponseType(typeof(CreateEdgeManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateEdgeManufacturerCommandResult> CreateEdgeManufacturer(
        [FromBody, Description("Параметры производителя кромок")] EdgeManufacturerModel model,
        [FromServices] ICommandHandler<CreateEdgeManufacturerCommand, CreateEdgeManufacturerCommandResult> handler) =>
        handler.Handle(new CreateEdgeManufacturerCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdgeManufacturer)),
     EndpointDescription("Обновление производителя кромок"),
     ProducesResponseType(typeof(UpdateEdgeManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateEdgeManufacturerCommandResult> UpdateEdgeManufacturer(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры производителя кромок")] EdgeManufacturerModel model,
        [FromServices] ICommandHandler<UpdateEdgeManufacturerCommand, UpdateEdgeManufacturerCommandResult> handler) =>
        handler.Handle(new UpdateEdgeManufacturerCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdgeManufacturer)),
     EndpointDescription("Удаление производителя кромок"),
     ProducesResponseType(typeof(DeleteEdgeManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeManufacturerCommandResult> DeleteEdgeManufacturer(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteEdgeManufacturerCommand, DeleteEdgeManufacturerCommandResult> handler) =>
        handler.Handle(new DeleteEdgeManufacturerCommand(id), HttpContext.RequestAborted);
}