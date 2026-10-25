using Catalog.Api.Features.Relations.Markings;
using Catalog.Api.Features.Relations.Markings.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Relations;

[Route("api/for-admin/relations/markings"),
 OpenApiTagOrder(16)]
public sealed class RelationMarkingsForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(RelationMarkings)),
     EndpointDescription("Список маркировок"),
     ProducesResponseType(typeof(GetRelationMarkingsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список маркировок")]
    public Task<GetRelationMarkingsQueryResult> RelationMarkings(
        [FromServices] IQueryHandler<GetRelationMarkingsQuery, GetRelationMarkingsQueryResult> handler) =>
        handler.Handle(new GetRelationMarkingsQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(RelationMarking)),
     EndpointDescription("Маркировка"),
     ProducesResponseType(typeof(RelationMarkingDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Маркировка")]
    public Task<RelationMarkingDto> RelationMarking(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetRelationMarkingQuery, RelationMarkingDto> handler) =>
        handler.Handle(new GetRelationMarkingQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateRelationMarking)),
     EndpointDescription("Создание маркировки"),
     ProducesResponseType(typeof(CreateRelationMarkingCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateRelationMarkingCommandResult> CreateRelationMarking(
        [FromBody, Description("Параметры маркировки")] RelationMarkingModel model,
        [FromServices] ICommandHandler<CreateRelationMarkingCommand, CreateRelationMarkingCommandResult> handler) =>
        handler.Handle(new CreateRelationMarkingCommand(model), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeSortingRelationMarking)),
     EndpointDescription("Перемещение маркировки"),
     ProducesResponseType(typeof(ChangeSortingRelationMarkingCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingRelationMarkingCommandResult> ChangeSortingRelationMarking(
        [FromBody, Description("Параметры перемещения")] ChangeSortingRelationMarkingCommand command,
        [FromServices] ICommandHandler<ChangeSortingRelationMarkingCommand, ChangeSortingRelationMarkingCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateRelationMarking)),
     EndpointDescription("Обновление маркировки"),
     ProducesResponseType(typeof(UpdateRelationMarkingCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateRelationMarkingCommandResult> UpdateRelationMarking(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры маркировки")] RelationMarkingModel model,
        [FromServices] ICommandHandler<UpdateRelationMarkingCommand, UpdateRelationMarkingCommandResult> handler) =>
        handler.Handle(new UpdateRelationMarkingCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteRelationMarking)),
     EndpointDescription("Удаление маркировки"),
     ProducesResponseType(typeof(DeleteRelationMarkingCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteRelationMarkingCommandResult> DeleteRelationMarking(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteRelationMarkingCommand, DeleteRelationMarkingCommandResult> handler) =>
        handler.Handle(new DeleteRelationMarkingCommand(id), HttpContext.RequestAborted);
}