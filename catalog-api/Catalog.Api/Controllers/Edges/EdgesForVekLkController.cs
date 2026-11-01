using Catalog.Api.Features.Edges.ForVekLk;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-vek-lk/edges"),
 OpenApiTagOrder(20),
 AllowAnonymous]
public sealed class EdgesForVekLkController : BaseApiController
{
    [HttpGet("article-for-1c"),
     EndpointSummary(nameof(ArticleFor1C)),
     EndpointDescription("Артикул кромки для выгрузки в 1С"),
     ProducesResponseType(typeof(GetEdgeArticleFor1CQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Артикул кромки"),
     ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson, Description = "Неверный артикул или неоднозначная замена")]
    public Task<GetEdgeArticleFor1CQueryResult> ArticleFor1C(
        [FromQuery, BindRequired, Description("Текущий артикул")] string article,
        [FromServices] IQueryHandler<GetEdgeArticleFor1CQuery, GetEdgeArticleFor1CQueryResult> handler) =>
        handler.Handle(new GetEdgeArticleFor1CQuery(article), HttpContext.RequestAborted);
}