using Catalog.Api.Features.Relations.ForAdmin;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Relations;

[Route("api/for-admin/relations"),
 OpenApiTagOrder(17)]
public sealed class MaterialEdgeRelationsForAdminController : BaseAdministrationApiController
{
    [HttpGet("compact"),
     EndpointSummary(nameof(CompactMaterialEdgeRelations)),
     EndpointDescription("Связи материалов и кромок"),
     ProducesResponseType(typeof(GetCompactMaterialEdgeRelationsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Связи и маркировки")]
    public Task<GetCompactMaterialEdgeRelationsQueryResult> CompactMaterialEdgeRelations(
        [FromServices] IQueryHandler<GetCompactMaterialEdgeRelationsQuery, GetCompactMaterialEdgeRelationsQueryResult> handler) =>
        handler.Handle(new GetCompactMaterialEdgeRelationsQuery(), HttpContext.RequestAborted);

    [HttpGet("materials/{materialId:int}/edges"),
     EndpointSummary(nameof(MaterialEdgeRelations)),
     EndpointDescription("Кромки материала"),
     ProducesResponseType(typeof(GetMaterialEdgeRelationsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Связи материала")]
    public Task<GetMaterialEdgeRelationsQueryResult> MaterialEdgeRelations(
        [FromRoute, Description("Id материала")] int materialId,
        [FromServices] IQueryHandler<GetMaterialEdgeRelationsQuery, GetMaterialEdgeRelationsQueryResult> handler) =>
        handler.Handle(new GetMaterialEdgeRelationsQuery(materialId), HttpContext.RequestAborted);

    [HttpGet("edges/{edgeId:int}/materials"),
     EndpointSummary(nameof(EdgeMaterialRelations)),
     EndpointDescription("Материалы кромки"),
     ProducesResponseType(typeof(GetMaterialEdgeRelationsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Связи кромки")]
    public Task<GetMaterialEdgeRelationsQueryResult> EdgeMaterialRelations(
        [FromRoute, Description("Id кромки")] int edgeId,
        [FromServices] IQueryHandler<GetEdgeMaterialRelationsQuery, GetMaterialEdgeRelationsQueryResult> handler) =>
        handler.Handle(new GetEdgeMaterialRelationsQuery(edgeId), HttpContext.RequestAborted);

    [HttpPut("materials/{materialId:int}/edges"),
     EndpointSummary(nameof(SetMaterialEdges)),
     EndpointDescription("Назначение кромок материала и их порядка"),
     ProducesResponseType(typeof(SetMaterialEdgesCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат изменения связей")]
    public Task<SetMaterialEdgesCommandResult> SetMaterialEdges(
        [FromRoute, Description("Id материала")] int materialId,
        [FromBody, Description("Кромки материала")] SetMaterialEdgesModel model,
        [FromServices] ICommandHandler<SetMaterialEdgesCommand, SetMaterialEdgesCommandResult> handler) =>
        handler.Handle(new SetMaterialEdgesCommand(materialId, model), HttpContext.RequestAborted);

    [HttpPut("materials/{materialId:int}/edges/{edgeId:int}/markings"),
     EndpointSummary(nameof(SetMaterialEdgeMarkings)),
     EndpointDescription("Назначение маркировок связи"),
     ProducesResponseType(typeof(SetMaterialEdgeMarkingsCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат изменения маркировок")]
    public Task<SetMaterialEdgeMarkingsCommandResult> SetMaterialEdgeMarkings(
        [FromRoute, Description("Id материала")] int materialId,
        [FromRoute, Description("Id кромки")] int edgeId,
        [FromBody, Description("Маркировки связи")] SetMaterialEdgeMarkingsModel model,
        [FromServices] ICommandHandler<SetMaterialEdgeMarkingsCommand, SetMaterialEdgeMarkingsCommandResult> handler) =>
        handler.Handle(new SetMaterialEdgeMarkingsCommand(materialId, edgeId, model), HttpContext.RequestAborted);

    [HttpPatch("edges/{edgeId:int}/materials"),
     EndpointSummary(nameof(UpdateEdgeMaterials)),
     EndpointDescription("Массовое изменение материалов кромки"),
     ProducesResponseType(typeof(UpdateEdgeMaterialsCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Количество изменённых материалов")]
    public Task<UpdateEdgeMaterialsCommandResult> UpdateEdgeMaterials(
        [FromRoute, Description("Id кромки")] int edgeId,
        [FromBody, Description("Изменения связей")] UpdateEdgeMaterialsModel model,
        [FromServices] ICommandHandler<UpdateEdgeMaterialsCommand, UpdateEdgeMaterialsCommandResult> handler) =>
        handler.Handle(new UpdateEdgeMaterialsCommand(edgeId, model), HttpContext.RequestAborted);
}