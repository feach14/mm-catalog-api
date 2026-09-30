using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UploadMaterialImageCommand(
    [property: FromForm, Description("Файл изображения")] IFormFile File,
    [property: FromForm, Description("Назначение изображения")] MaterialImageTypeEnum ImageType)
    : ICommand<UploadMaterialImageCommandResult>;

public sealed class UploadMaterialImageCommandResult : CachedFileDto;

public sealed class UploadMaterialImageCommandValidator : AbstractValidator<UploadMaterialImageCommand>
{
    private const int MaxImageSize = 5 * 1024 * 1024;

    public UploadMaterialImageCommandValidator()
    {
        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Файл изображения не передан")
            .Must(file => file.Length > 0).WithMessage("Файл изображения пустой")
            .Must(file => file.Length <= MaxImageSize).WithMessage("Размер изображения не должен превышать 5 МБ")
            .MustAsync(async (file, ct) =>
            {
                await using var stream = file.OpenReadStream();
                var header = new byte[12];
                var bytesRead = await stream.ReadAsync(header, ct);
                return GetImageContentType(header.AsSpan(0, bytesRead)) is not null;
            })
            .WithMessage("Допустимы только изображения PNG, JPEG и WebP");

        RuleFor(x => x.ImageType)
            .IsInEnum()
            .WithMessage("Указано неизвестное назначение изображения");
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

public class UploadMaterialImageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UploadMaterialImageCommand, UploadMaterialImageCommandResult>
{
    public async Task<UploadMaterialImageCommandResult> Handle(UploadMaterialImageCommand command, CancellationToken ct)
    {
        await using var stream = command.File.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var data = memoryStream.ToArray();
        var contentType = UploadMaterialImageCommandValidator.GetImageContentType(data)
                          ?? throw new BadHttpRequestException("Допустимы только изображения PNG, JPEG и WebP.");

        var cachedFile = new UploadMaterialImageCommandResult
        {
            FileName = command.File.FileName,
            FileGuid = Guid.NewGuid(),
            ContentType = contentType,
            ImageType = command.ImageType
        };

        await dbContext.ImageCache.AddAsync(new ImageCache
        {
            Guid = cachedFile.FileGuid,
            FileName = cachedFile.FileName,
            Type = cachedFile.ContentType,
            Data = data,
            ImageType = cachedFile.ImageType
        }, ct);
        await dbContext.SaveChangesAsync(ct);

        return cachedFile;
    }
}