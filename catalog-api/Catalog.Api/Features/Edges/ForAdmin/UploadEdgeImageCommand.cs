using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record UploadEdgeImageCommand(
    [property: FromForm, Description("Файл изображения")] IFormFile File,
    [property: FromForm, Description("Назначение изображения")] EdgeImageTypeEnum ImageType)
    : ICommand<UploadEdgeImageCommandResult>;

public sealed record UploadEdgeImageCommandResult(
    [property: Description("Название файла")] string FileName,
    [property: Description("Guid файла")] Guid FileGuid,
    [property: Description("Тип файла")] string ContentType,
    [property: Description("Назначение изображения")] EdgeImageTypeEnum ImageType);

public sealed class UploadEdgeImageCommandValidator : AbstractValidator<UploadEdgeImageCommand>
{
    private const int MaxImageSize = 2 * 1024 * 1024;

    public UploadEdgeImageCommandValidator()
    {
        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Файл изображения не передан")
            .Must(file => file.FileName.Length <= 255).WithMessage("Название файла не должно превышать 255 символов")
            .Must(file => file.Length > 0).WithMessage("Файл изображения пустой")
            .Must(file => file.Length <= MaxImageSize).WithMessage("Размер изображения не должен превышать 2 МБ")
            .MustAsync(async (file, ct) =>
            {
                await using var stream = file.OpenReadStream();
                var header = new byte[12];
                var bytesRead = await stream.ReadAtLeastAsync(header, header.Length, false, ct);
                return GetImageContentType(header.AsSpan(0, bytesRead)) is not null;
            })
            .WithMessage("Допустимы только изображения PNG, JPEG и WebP");

        RuleFor(x => x.ImageType)
            .IsInEnum().WithMessage("Указано неизвестное назначение изображения");
    }

    internal static string? GetImageContentType(ReadOnlySpan<byte> data)
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

public class UploadEdgeImageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UploadEdgeImageCommand, UploadEdgeImageCommandResult>
{
    public async Task<UploadEdgeImageCommandResult> Handle(UploadEdgeImageCommand command, CancellationToken ct)
    {
        await using var stream = command.File.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var data = memoryStream.ToArray();
        var contentType = UploadEdgeImageCommandValidator.GetImageContentType(data)
                          ?? throw new BadHttpRequestException("Допустимы только изображения PNG, JPEG и WebP.");
        var result = new UploadEdgeImageCommandResult(command.File.FileName, Guid.NewGuid(), contentType, command.ImageType);

        await dbContext.EdgeImageCache.AddAsync(new EdgeImageCache
        {
            Guid = result.FileGuid,
            FileName = result.FileName,
            Type = result.ContentType,
            Data = data,
            ImageType = result.ImageType
        }, ct);
        await dbContext.SaveChangesAsync(ct);

        return result;
    }
}