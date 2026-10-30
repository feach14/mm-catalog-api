using Catalog.Api.Features.Materials.ForVekLk;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/for-vek-lk"),
 OpenApiTagOrder(19),
 Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class MaterialsForVekLkController : BaseApiController
{
    [HttpGet("materials"),
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Каталог материалов для раскроя"),
     ProducesResponseType(typeof(GetCuttingMaterialCatalogQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы и категории для раскроя"),
     ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Необходима авторизация")]
    public Task<GetCuttingMaterialCatalogQueryResult> Materials(
        [FromServices] IQueryHandler<GetCuttingMaterialCatalogQuery, GetCuttingMaterialCatalogQueryResult> handler) =>
        handler.Handle(new GetCuttingMaterialCatalogQuery(), HttpContext.RequestAborted);

    [HttpGet("materials/{id:int}"),
     EndpointSummary(nameof(Material)),
     EndpointDescription("Материал для раскроя"),
     ProducesResponseType(typeof(GetCuttingMaterialQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал"),
     ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson, Description = "Материал не найден"),
     ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Необходима авторизация")]
    public Task<GetCuttingMaterialQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetCuttingMaterialQuery, GetCuttingMaterialQueryResult> handler) =>
        handler.Handle(new GetCuttingMaterialQuery(id), HttpContext.RequestAborted);
}