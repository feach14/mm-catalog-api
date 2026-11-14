using Catalog.Api.Features.Edges.Sizes;
using Catalog.Api.Features.Edges.Sizes.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/sizes"),
 OpenApiTagOrder(13)]
public sealed class EdgeSizesForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeSizes)),
     EndpointDescription("Список размеров кромок"),
     ProducesResponseType(typeof(GetEdgeSizesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список размеров кромок")]
    public Task<GetEdgeSizesQueryResult> EdgeSizes(
        [FromServices] IQueryHandler<GetEdgeSizesQuery, GetEdgeSizesQueryResult> handler) =>
        handler.Handle(new GetEdgeSizesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(EdgeSize)),
     EndpointDescription("Размер кромок"),
     ProducesResponseType(typeof(EdgeSizeDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Размер кромок")]
    public Task<EdgeSizeDto> EdgeSize(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetEdgeSizeQuery, EdgeSizeDto> handler) =>
        handler.Handle(new GetEdgeSizeQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdgeSize)),
     EndpointDescription("Создание размера кромок"),
     ProducesResponseType(typeof(CreateEdgeSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateEdgeSizeCommandResult> CreateEdgeSize(
        [FromBody, Description("Параметры размера кромок")] EdgeSizeModel model,
        [FromServices] ICommandHandler<CreateEdgeSizeCommand, CreateEdgeSizeCommandResult> handler) =>
        handler.Handle(new CreateEdgeSizeCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdgeSize)),
     EndpointDescription("Обновление размера кромок"),
     ProducesResponseType(typeof(UpdateEdgeSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateEdgeSizeCommandResult> UpdateEdgeSize(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры размера кромок")] EdgeSizeModel model,
        [FromServices] ICommandHandler<UpdateEdgeSizeCommand, UpdateEdgeSizeCommandResult> handler) =>
        handler.Handle(new UpdateEdgeSizeCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdgeSize)),
     EndpointDescription("Удаление размера кромок"),
     ProducesResponseType(typeof(DeleteEdgeSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeSizeCommandResult> DeleteEdgeSize(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteEdgeSizeCommand, DeleteEdgeSizeCommandResult> handler) =>
        handler.Handle(new DeleteEdgeSizeCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка размеров кромок"),
     ProducesResponseType(typeof(ChangeSortingEdgeSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingEdgeSizeCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingEdgeSizeCommand model,
        [FromServices] ICommandHandler<ChangeSortingEdgeSizeCommand, ChangeSortingEdgeSizeCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}