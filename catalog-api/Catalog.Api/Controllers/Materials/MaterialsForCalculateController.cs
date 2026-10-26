using Catalog.Api.Features.Edges.ForCalculate;
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
    Roles = AuthConstants.AdministrationRoles)]
public class MaterialsForCalculateController : BaseApiController
{
    [HttpGet("materials"),
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Материалы"),
     ProducesResponseType(typeof(GetMaterialsForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы"),
     ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
     ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав тестера, менеджера или администратора")]
    public Task<GetMaterialsForCalculateQueryResult> Materials(
        [FromServices] IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult> handler) =>
        handler.Handle(new GetMaterialsForCalculateQuery(), HttpContext.RequestAborted);

    [HttpGet("materials/{id:int}"),
     AllowAnonymous,
     EndpointSummary(nameof(Material)),
     EndpointDescription("Материал для расчёта"),
     ProducesResponseType(typeof(GetMaterialForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialForCalculateQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialForCalculateQuery, GetMaterialForCalculateQueryResult> handler) =>
        handler.Handle(new GetMaterialForCalculateQuery(id), HttpContext.RequestAborted);

    [HttpGet("edges/price"),
     AllowAnonymous,
     EndpointSummary(nameof(EdgePrice)),
     EndpointDescription("Стоимость кромки для расчёта"),
     ProducesResponseType(typeof(GetEdgePriceForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Стоимость кромки"),
     ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson, Description = "Неверные параметры или не найдена единственная подходящая кромка")]
    public Task<GetEdgePriceForCalculateQueryResult> EdgePrice(
        [FromQuery] GetEdgePriceForCalculateQuery query,
        [FromServices] IQueryHandler<GetEdgePriceForCalculateQuery, GetEdgePriceForCalculateQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpPost("materials/by-ids"),
     EndpointSummary(nameof(MaterialsByIds)),
     EndpointDescription("Материалы по Id"),
     ProducesResponseType(typeof(GetMaterialsByIdsForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы"),
     ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
     ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав тестера, менеджера или администратора")]
    public Task<GetMaterialsByIdsForCalculateQueryResult> MaterialsByIds(
        [FromBody, Description("Материалы по Id")] GetMaterialsByIdsForCalculateQuery query,
        [FromServices] IQueryHandler<GetMaterialsByIdsForCalculateQuery, GetMaterialsByIdsForCalculateQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);
}