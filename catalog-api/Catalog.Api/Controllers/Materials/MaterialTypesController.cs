using Catalog.Api.Features.MaterialTypes.Public;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Materials;

[Route("api/materials/material-types"),
 OpenApiTagOrder(8),
 AllowAnonymous]
public sealed class MaterialTypesController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(MaterialTypes)),
     EndpointDescription("Полный публичный список типов материалов, включая типы без материалов"),
     ProducesResponseType(typeof(GetPublicMaterialTypesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список типов материалов в порядке каталога")]
    public Task<GetPublicMaterialTypesQueryResult> MaterialTypes(
        [FromServices] IQueryHandler<GetPublicMaterialTypesQuery, GetPublicMaterialTypesQueryResult> handler) =>
        handler.Handle(new GetPublicMaterialTypesQuery(), HttpContext.RequestAborted);
}