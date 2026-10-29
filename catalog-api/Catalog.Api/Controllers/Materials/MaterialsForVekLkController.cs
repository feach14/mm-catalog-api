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
}