using Core;
using Core.Attributes;
using Core.Controllers;

namespace Catalog.Api.Controllers;

[Route("/api/catalog/materials")]
[OpenApiTagOrder(1)]
[Authorize(AuthenticationSchemes = CoreAppConstants.InternalAuthenticationSchemeName)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public class MaterialsController : BaseApiController
{
}
