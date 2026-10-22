using Catalog.Api.Features.History;
using Core.AccountAuth;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers;

[Route("api/for-admin/catalog/history"),
 OpenApiTagOrder(10)]
public sealed class CatalogHistoryForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(History)),
     EndpointDescription("Постраничная история изменений всего каталога"),
     ProducesResponseType(typeof(GetCatalogHistoryQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "История изменений каталога")]
    public Task<GetCatalogHistoryQueryResult> History(
        [FromQuery] GetCatalogHistoryQuery query,
        [FromServices] IQueryHandler<GetCatalogHistoryQuery, GetCatalogHistoryQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpDelete("test-runs/{runId:int}"),
     ApiExplorerSettings(IgnoreApi = true),
     Authorize(Roles = AuthConstants.TesterRoleName),
     EndpointSummary(nameof(DeleteTestRunHistory)),
     EndpointDescription("Удаление записей истории, созданных изолированным тестовым прогоном"),
     ProducesResponseType(typeof(DeleteCatalogTestRunHistoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат очистки тестовой истории"),
     ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет роли тестера")]
    public Task<DeleteCatalogTestRunHistoryCommandResult> DeleteTestRunHistory(
        [FromRoute, Description("Числовой id тестового HIST-прогона")] int runId,
        [FromServices] ICommandHandler<DeleteCatalogTestRunHistoryCommand, DeleteCatalogTestRunHistoryCommandResult> handler) =>
        handler.Handle(new DeleteCatalogTestRunHistoryCommand(runId), HttpContext.RequestAborted);
}