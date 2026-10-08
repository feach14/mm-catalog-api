using Catalog.Api.Features.MaterialCategories.ForCalculate;
using Catalog.Api.Features.Materials.ForCalculate;
using Core.AccountAuth;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/calculate"),
 OpenApiTagOrder(10),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AuthConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав тестера, менеджера или администратора")]
public class CalculateMaterialsController : BaseApiController
{
    [HttpGet("material-categories"),
     EndpointSummary(nameof(MaterialCategories)),
     EndpointDescription("Категории материалов для CalculateAPI"),
     ProducesResponseType(typeof(GetMaterialCategoriesForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Все категории материалов")]
    public Task<GetMaterialCategoriesForCalculateQueryResult> MaterialCategories(
        [FromServices] IQueryHandler<GetMaterialCategoriesForCalculateQuery, GetMaterialCategoriesForCalculateQueryResult> handler) =>
        handler.Handle(new GetMaterialCategoriesForCalculateQuery(), HttpContext.RequestAborted);

    [HttpGet("materials"),
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Материалы категории для CalculateAPI"),
     ProducesResponseType(typeof(GetMaterialsForCalculateQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Все материалы выбранной категории")]
    public Task<GetMaterialsForCalculateQueryResult> Materials(
        [FromQuery] GetMaterialsForCalculateQuery query,
        [FromServices] IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);
}