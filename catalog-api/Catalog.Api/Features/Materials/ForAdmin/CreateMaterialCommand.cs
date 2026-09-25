using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record CreateMaterialCommand(MaterialModel Material) : ICommand<CreateMaterialCommandResult>;

public sealed record CreateMaterialCommandResult([property:Description("Id материала")]int Id);

public class CreateMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult>
{
    public async Task<CreateMaterialCommandResult> Handle(CreateMaterialCommand command, CancellationToken ct)
    {
        var materialName = command.Material.Name.Trim();
        if (await dbContext.Materials.AnyAsync(x => x.Name == materialName, ct))
            throw new BadHttpRequestException("Материал с таким названием уже существует.");

        var material = new Material
        {
            CategoryId = command.Material.CategoryId,
            Article = command.Material.Article,
            Name = command.Material.Name,
            Size = command.Material.Size,
            Depth = command.Material.Depth,
            KvM = command.Material.KvM,
            PerimetrM = command.Material.PerimetrM,
            Count = command.Material.Count,
            ApplicableToRaskroys = command.Material.ApplicableToRaskroys,
            ApplicableToPvhFacades = command.Material.ApplicableToPvhFacades,
            ApplicableToEmalFacades = command.Material.ApplicableToEmalFacades,
            CommentOnMaterialIsRequired = command.Material.CommentOnMaterialIsRequired,
            AllowSecondItemInOrder = command.Material.AllowSecondItemInOrder,
            ExternalLink = command.Material.ExternalLink,
            Price = command.Material.Price,
            CountTypeEnum = command.Material.CountTypeEnum,
            OrderByCol = await dbContext.Materials.Where(y => y.CategoryId == command.Material.CategoryId).MaxAsync(x => x.OrderByCol, ct) + 1
        };
        await dbContext.Materials.AddAsync(material, ct);
        await dbContext.SaveChangesAsync(ct);

        if (command.Material.Image != null)
        {
            var fileFromCache = memoryCache.Get<CachedFileDto>(command.Material.Image);
            if(fileFromCache is null)
                throw new ApplicationException("Файл с изображением материала отсутствует в кэше");
            
            await dbContext.MaterialImages.AddAsync(
                new MaterialImage {
                    Data = fileFromCache.Data,
                    Guid = fileFromCache.FileGuid,
                    MaterialId = material.Id,
                    Type = fileFromCache.ContentType
                }, ct);
        }
        
        await dbContext.SaveChangesAsync(ct);

        return new CreateMaterialCommandResult(material.Id);
    }
}