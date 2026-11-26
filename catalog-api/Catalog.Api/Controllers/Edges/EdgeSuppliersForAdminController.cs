using Catalog.Api.Features.Edges.Suppliers;
using Catalog.Api.Features.Edges.Suppliers.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges/suppliers"),
 OpenApiTagOrder(13)]
public sealed class EdgeSuppliersForAdminController : BaseAdministrationApiController
{
    [HttpGet,
     EndpointSummary(nameof(EdgeSuppliers)),
     EndpointDescription("Список поставщиков кромок"),
     ProducesResponseType(typeof(GetEdgeSuppliersQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список поставщиков кромок")]
    public Task<GetEdgeSuppliersQueryResult> EdgeSuppliers(
        [FromServices] IQueryHandler<GetEdgeSuppliersQuery, GetEdgeSuppliersQueryResult> handler) =>
        handler.Handle(new GetEdgeSuppliersQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(EdgeSupplier)),
     EndpointDescription("Поставщик кромок"),
     ProducesResponseType(typeof(EdgeSupplierDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Поставщик кромок")]
    public Task<EdgeSupplierDto> EdgeSupplier(
        [FromRoute, Description("Id")] int id,
        [FromServices] IQueryHandler<GetEdgeSupplierQuery, EdgeSupplierDto> handler) =>
        handler.Handle(new GetEdgeSupplierQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdgeSupplier)),
     EndpointDescription("Создание поставщика кромок"),
     ProducesResponseType(typeof(CreateEdgeSupplierCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateEdgeSupplierCommandResult> CreateEdgeSupplier(
        [FromBody, Description("Параметры поставщика кромок")] EdgeSupplierModel model,
        [FromServices] ICommandHandler<CreateEdgeSupplierCommand, CreateEdgeSupplierCommandResult> handler) =>
        handler.Handle(new CreateEdgeSupplierCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdgeSupplier)),
     EndpointDescription("Обновление поставщика кромок"),
     ProducesResponseType(typeof(UpdateEdgeSupplierCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateEdgeSupplierCommandResult> UpdateEdgeSupplier(
        [FromRoute, Description("Id")] int id,
        [FromBody, Description("Параметры поставщика кромок")] EdgeSupplierModel model,
        [FromServices] ICommandHandler<UpdateEdgeSupplierCommand, UpdateEdgeSupplierCommandResult> handler) =>
        handler.Handle(new UpdateEdgeSupplierCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdgeSupplier)),
     EndpointDescription("Удаление поставщика кромок"),
     ProducesResponseType(typeof(DeleteEdgeSupplierCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeSupplierCommandResult> DeleteEdgeSupplier(
        [FromRoute, Description("Id")] int id,
        [FromServices] ICommandHandler<DeleteEdgeSupplierCommand, DeleteEdgeSupplierCommandResult> handler) =>
        handler.Handle(new DeleteEdgeSupplierCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка поставщиков кромок"),
     ProducesResponseType(typeof(ChangeSortingEdgeSupplierCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingEdgeSupplierCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingEdgeSupplierCommand model,
        [FromServices] ICommandHandler<ChangeSortingEdgeSupplierCommand, ChangeSortingEdgeSupplierCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}