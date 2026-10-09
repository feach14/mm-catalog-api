using Catalog.Api.Features.Materials.ForCalculate;
using Core.AccountAuth;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/for-calculate"),
 OpenApiTagOrder(3),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AuthConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав тестера, менеджера или администратора")]
public class MaterialsForCalculateController : BaseApiController
{
    [HttpGet("materials"),
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Материалы"),
     ProducesResponseType(typeof(GetMaterialsForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы")]
    public Task<GetMaterialsForCalculateQueryResult> Materials(
        [FromServices] IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult> handler) =>
        handler.Handle(new GetMaterialsForCalculateQuery(), HttpContext.RequestAborted);

    [HttpPost("materials/by-ids"),
     EndpointSummary(nameof(MaterialsByIds)),
     EndpointDescription("Материалы по Id"),
     ProducesResponseType(typeof(GetMaterialsByIdsForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы")]
    public Task<GetMaterialsByIdsForCalculateQueryResult> MaterialsByIds(
        [FromBody, Description("Материалы по Id")] GetMaterialsByIdsForCalculateQuery query,
        [FromServices] IQueryHandler<GetMaterialsByIdsForCalculateQuery, GetMaterialsByIdsForCalculateQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);
}