using Catalog.Api.Features.Edges.ArticleReplacements;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/article-replacements"),
 OpenApiTagOrder(18)]
public sealed class EdgeArticleReplacementsForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeArticleReplacements)),
     EndpointDescription("Список замен артикулов кромок"),
     ProducesResponseType(typeof(GetEdgeArticleReplacementsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список замен артикулов")]
    public Task<GetEdgeArticleReplacementsQueryResult> EdgeArticleReplacements(
        [FromServices] IQueryHandler<GetEdgeArticleReplacementsQuery, GetEdgeArticleReplacementsQueryResult> handler) =>
        handler.Handle(new GetEdgeArticleReplacementsQuery(), HttpContext.RequestAborted);

    [HttpDelete("{edgeId:int}"),
     EndpointSummary(nameof(DeleteEdgeArticleReplacements)),
     EndpointDescription("Удаление замен артикулов кромки"),
     ProducesResponseType(typeof(DeleteEdgeArticleReplacementsCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeArticleReplacementsCommandResult> DeleteEdgeArticleReplacements(
        [FromRoute, Description("Id кромки")] int edgeId,
        [FromServices] ICommandHandler<DeleteEdgeArticleReplacementsCommand, DeleteEdgeArticleReplacementsCommandResult> handler) =>
        handler.Handle(new DeleteEdgeArticleReplacementsCommand(edgeId), HttpContext.RequestAborted);
}