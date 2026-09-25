using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateMaterialCommand(int Id, MaterialModel Material) : ICommand<UpdateMaterialCommandResult>;

public sealed record UpdateMaterialCommandResult(
    [property:Description("Успех операции")] bool Success);

public class UpdateMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult>
{
    public async Task<UpdateMaterialCommandResult> Handle(UpdateMaterialCommand command, CancellationToken ct)
    {
        var material =
            await dbContext.Materials
                .Include(x => x.Category)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не существует.");

        var materialName = command.Material.Name.Trim();
        if (await dbContext.Materials.AnyAsync(x => x.Name == materialName && x.Id != command.Id, ct))
            throw new BadHttpRequestException("Материал с таким названием уже существует.");

        if (material.DefaultPvhFacade && !command.Material.ApplicableToPvhFacades)
            throw new BadHttpRequestException("Материал по умолчанию для калькулятора фасадов ПВХ. Должно быть выставлено 'Применимо к фасадам ПВХ'");

        if (material.DefaultEmalFacade && !command.Material.ApplicableToEmalFacades)
            throw new BadHttpRequestException("Материал по умолчанию для калькулятора фасадов Эмаль. Должно быть выставлено 'Применимо к фасадам Эмаль'");
        
        material.CategoryId = command.Material.CategoryId;
        material.Article = command.Material.Article;
        material.Name = command.Material.Name;
        material.Size = command.Material.Size;
        material.Depth = command.Material.Depth;
        material.KvM = command.Material.KvM;
        material.PerimetrM = command.Material.PerimetrM;
        material.Count = command.Material.Count;
        material.ApplicableToRaskroys = command.Material.ApplicableToRaskroys;
        material.ApplicableToPvhFacades = command.Material.ApplicableToPvhFacades;
        material.ApplicableToEmalFacades = command.Material.ApplicableToEmalFacades;
        material.CommentOnMaterialIsRequired = command.Material.CommentOnMaterialIsRequired;
        material.AllowSecondItemInOrder = command.Material.AllowSecondItemInOrder;
        material.ExternalLink = command.Material.ExternalLink;
        material.Price = command.Material.Price;
        material.CountTypeEnum = command.Material.CountTypeEnum;

        // Удаляем старые изображения
        material.Images
            .Where(img => command.Material.Image != img.Guid)
            .ToList()
            .ForEach(removeImg => material.Images.Remove(removeImg));
        
        // Сохраняем новые изображения
        if (command.Material.Image != null && material.Images.All(x => x.Guid != command.Material.Image))
        {
            var cachedFile = memoryCache.Get<CachedFileDto>(command.Material.Image);
            if (cachedFile is null)
                throw new ApplicationException($"Файл {command.Material.Image} отсутствует в кэше");

            material.Images.Add(new MaterialImage
            {
                Data = cachedFile.Data,
                Type = cachedFile.ContentType,
                Guid = command.Material.Image.Value,
                MaterialId = material.Id
            });
        }
        
        dbContext.Materials.Update(material);

        await dbContext.SaveChangesAsync(ct);
        
        return new UpdateMaterialCommandResult(true);
    }
}