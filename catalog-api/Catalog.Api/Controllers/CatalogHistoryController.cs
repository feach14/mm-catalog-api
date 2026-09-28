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

    [HttpDelete("test-runs/{runId}")]
    [Authorize(Roles = AppConstants.TesterRoleName)]
    [EndpointSummary(nameof(DeleteTestRunHistory))]
    [EndpointDescription("Удаление записей истории, созданных изолированным тестовым прогоном")]
    [ProducesResponseType(typeof(DeleteCatalogTestRunHistoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат очистки тестовой истории")]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest, Description = "Идентификатор тестового прогона имеет неверный формат")]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет роли тестера")]
    public Task<DeleteCatalogTestRunHistoryCommandResult> DeleteTestRunHistory(
        [FromRoute, Description("Числовой id тестового HIST-прогона")] int runId,
        [FromServices] ICommandHandler<DeleteCatalogTestRunHistoryCommand, DeleteCatalogTestRunHistoryCommandResult> handler) =>
        handler.Handle(new DeleteCatalogTestRunHistoryCommand(runId), HttpContext.RequestAborted);
}