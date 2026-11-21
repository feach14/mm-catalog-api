namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public sealed record MaterialCommentDto(
    [property: Description("Id")] int Id,
    [property: Description("Текст комментария")] string Text,
    [property: Description("Комментарий для клиента")] bool IsForClient);