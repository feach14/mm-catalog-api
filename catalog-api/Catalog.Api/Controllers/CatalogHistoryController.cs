using Catalog.Api.Features.History;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers;

[Route("api/catalog/history")]
[OpenApiTagOrder(5)]
[Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AppConstants.AdministrationRoles)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
[ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав тестера, менеджера или администратора")]
public sealed class CatalogHistoryController : BaseApiController
{
    [HttpGet]
    [EndpointSummary(nameof(History))]
    [EndpointDescription("Пагинированная история изменений всего каталога")]
    [ProducesResponseType(typeof(GetCatalogHistoryQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public Task<GetCatalogHistoryQueryResult> History(
        [FromQuery] GetCatalogHistoryQuery query,
        [FromServices] IQueryHandler<GetCatalogHistoryQuery, GetCatalogHistoryQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);
}