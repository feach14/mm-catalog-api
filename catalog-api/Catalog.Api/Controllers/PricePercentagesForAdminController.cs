using Catalog.Api.Features.PricePercentages;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers;

[Route("api/for-admin/price-percentages"),
 OpenApiTagOrder(19)]
public sealed class PricePercentagesForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(PricePercentages)),
     EndpointDescription("Проценты для цен материалов и кромок"),
     ProducesResponseType(typeof(GetPricePercentagesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Проценты для цен")]
    public Task<GetPricePercentagesQueryResult> PricePercentages(
        [FromQuery, Description("Параметры отбора процентов")] GetPricePercentagesQuery query,
        [FromServices] IQueryHandler<GetPricePercentagesQuery, GetPricePercentagesQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpPut("{key}"),
     EndpointSummary(nameof(UpdatePricePercentage)),
     EndpointDescription("Изменение процента для цен"),
     ProducesResponseType(typeof(UpdatePricePercentageCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdatePricePercentageCommandResult> UpdatePricePercentage(
        [FromRoute, Description("Код")] string key,
        [FromBody, Description("Значение в процентах")] UpdatePricePercentageModel model,
        [FromServices] ICommandHandler<UpdatePricePercentageCommand, UpdatePricePercentageCommandResult> handler) =>
        handler.Handle(new UpdatePricePercentageCommand(key, model), HttpContext.RequestAborted);
}