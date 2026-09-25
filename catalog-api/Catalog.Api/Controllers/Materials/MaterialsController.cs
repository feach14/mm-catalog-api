namespace Catalog.Api.Controllers.Materials;

using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Features.Materials;
using Features.Materials.ForAdmin;
using Features.Materials.ForAdmin.Dto;
using Features.Materials.Public;
using Features.Materials.Public.Dto;
using Microsoft.AspNetCore.Authentication.Cookies;

[Route("api/materials")]
[OpenApiTagOrder(1)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public class MaterialsController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary(nameof(Materials))]
    [EndpointDescription("Публичный список материалов")]
    [ProducesResponseType(typeof(GetAllMaterialsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы и их категории")]
    public Task<GetAllMaterialsQueryResult> Materials(
        [FromQuery] GetAllMaterialsQuery query,
        [FromServices] IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(Material))]
    [EndpointDescription("Публичная информация о материале")]
    [ProducesResponseType(typeof(GetMaterialQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialQuery, GetMaterialQueryResult> handler) =>
        handler.Handle(new GetMaterialQuery(id), HttpContext.RequestAborted);

    [HttpGet("admin")]
    [EndpointSummary(nameof(MaterialsForAdminPanel))]
    [EndpointDescription("Список материалов для административной панели")]
    [ProducesResponseType(typeof(GetAllMaterialsForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список материалов")]
    public Task<GetAllMaterialsForAdminQueryResult> MaterialsForAdminPanel(
        [FromQuery] GetAllMaterialsForAdminQuery query,
        [FromServices] IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet("admin/{id:int}")]
    [EndpointSummary(nameof(MaterialForAdminPanel))]
    [EndpointDescription("Информация о материале для административной панели")]
    [ProducesResponseType(typeof(GetMaterialForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialForAdminQueryResult> MaterialForAdminPanel(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult> handler) =>
        handler.Handle(new GetMaterialForAdminQuery(id), HttpContext.RequestAborted);

    [HttpPost]
    [EndpointSummary(nameof(CreateMaterial))]
    [EndpointDescription("Создание нового материала")]
    [ProducesResponseType(typeof(CreateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialCommandResult> CreateMaterial(
        [FromBody, Description("Параметры материала")] MaterialModel model,
        [FromServices] ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult> handler) =>
        handler.Handle(new CreateMaterialCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}")]
    [EndpointSummary(nameof(UpdateMaterial))]
    [EndpointDescription("Обновление материала")]
    [ProducesResponseType(typeof(UpdateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialCommandResult> UpdateMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromBody, Description("Параметры материала")] MaterialModel model,
        [FromServices] ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult> handler) =>
        handler.Handle(new UpdateMaterialCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}")]
    [EndpointSummary(nameof(DeleteMaterial))]
    [EndpointDescription("Удаление материала")]
    [ProducesResponseType(typeof(DeleteMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialCommandResult> DeleteMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult> handler) =>
        handler.Handle(new DeleteMaterialCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col")]
    [EndpointSummary(nameof(ChangeOrderCol))]
    [EndpointDescription("Изменение порядка сортировки материалов в категории")]
    [ProducesResponseType(typeof(ChangeSortingMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageRequestSize)]
    [EndpointSummary(nameof(UploadMaterialImage))]
    [EndpointDescription("Загрузка изображения материала в формате PNG, JPEG или WebP размером до 5 МБ")]
    [ProducesResponseType(typeof(UploadMaterialImageCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Данные загруженного изображения")]
    public async Task<UploadMaterialImageCommandResult> UploadMaterialImage(
        [FromForm, Description("Файл изображения")] IFormFile file,
        [FromServices] ICommandHandler<UploadMaterialImageCommand, UploadMaterialImageCommandResult> handler)
    {
        const int maxImageSize = 5 * 1024 * 1024;

        if (file.Length == 0)
            throw new BadHttpRequestException("Файл изображения пустой.");

        if (file.Length > maxImageSize)
            throw new BadHttpRequestException("Размер изображения не должен превышать 5 МБ.");

        await using var stream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, HttpContext.RequestAborted);

        var data = memoryStream.ToArray();
        var contentType = GetImageContentType(data)
                          ?? throw new BadHttpRequestException("Допустимы только изображения PNG, JPEG и WebP.");

        return await handler.Handle(
            new UploadMaterialImageCommand(file.FileName, data, contentType),
            HttpContext.RequestAborted);
    }

    [HttpGet("images/{fileGuid:guid}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialImage))]
    [EndpointDescription("Изображение материала")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, Description = "Изображение материала")]
    public async Task<FileContentResult> MaterialImage(
        [FromRoute, Description("Id изображения")] Guid fileGuid,
        [FromServices] IQueryHandler<GetMaterialImageQuery, GetMaterialImageQueryResult> handler)
    {
        var image = await handler.Handle(new GetMaterialImageQuery(fileGuid), HttpContext.RequestAborted);
        return File(image.Data, image.ContentType);
    }

    private const long MaxImageRequestSize = 6 * 1024 * 1024;

    private static string? GetImageContentType(byte[] data)
    {
        if (data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return MediaTypeNames.Image.Png;

        if (data.Length >= 3 && data[..3].SequenceEqual(new byte[] { 255, 216, 255 }))
            return MediaTypeNames.Image.Jpeg;

        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }
}
