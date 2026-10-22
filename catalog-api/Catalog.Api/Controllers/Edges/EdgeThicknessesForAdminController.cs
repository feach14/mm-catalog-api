using Catalog.Api.Features.Edges.Thicknesses;
using Catalog.Api.Features.Edges.Thicknesses.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/thicknesses"),
 OpenApiTagOrder(13)]
public sealed class EdgeThicknessesForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeThicknesses)),
     EndpointDescription("Список толщин кромок"),
     ProducesResponseType(typeof(GetEdgeThicknessesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список толщин кромок")]
    public Task<GetEdgeThicknessesQueryResult> EdgeThicknesses(
        [FromServices] IQueryHandler<GetEdgeThicknessesQuery, GetEdgeThicknessesQueryResult> handler) =>
        handler.Handle(new GetEdgeThicknessesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(EdgeThickness)),
     EndpointDescription("Толщина кромок"),
     ProducesResponseType(typeof(EdgeThicknessDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Толщина кромок")]
    public Task<EdgeThicknessDto> EdgeThickness(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetEdgeThicknessQuery, EdgeThicknessDto> handler) =>
        handler.Handle(new GetEdgeThicknessQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdgeThickness)),
     EndpointDescription("Создание толщины кромок"),
     ProducesResponseType(typeof(CreateEdgeThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateEdgeThicknessCommandResult> CreateEdgeThickness(
        [FromBody, Description("Параметры толщины кромок")] EdgeThicknessModel model,
        [FromServices] ICommandHandler<CreateEdgeThicknessCommand, CreateEdgeThicknessCommandResult> handler) =>
        handler.Handle(new CreateEdgeThicknessCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdgeThickness)),
     EndpointDescription("Обновление толщины кромок"),
     ProducesResponseType(typeof(UpdateEdgeThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateEdgeThicknessCommandResult> UpdateEdgeThickness(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры толщины кромок")] EdgeThicknessModel model,
        [FromServices] ICommandHandler<UpdateEdgeThicknessCommand, UpdateEdgeThicknessCommandResult> handler) =>
        handler.Handle(new UpdateEdgeThicknessCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdgeThickness)),
     EndpointDescription("Удаление толщины кромок"),
     ProducesResponseType(typeof(DeleteEdgeThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeThicknessCommandResult> DeleteEdgeThickness(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteEdgeThicknessCommand, DeleteEdgeThicknessCommandResult> handler) =>
        handler.Handle(new DeleteEdgeThicknessCommand(id), HttpContext.RequestAborted);
}