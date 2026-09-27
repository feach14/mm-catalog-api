namespace Catalog.Api.Features.Materials.ForAdmin;

using Core.CQRS;
using Database;
using Database.Entities;
using Dto;
using Microsoft.Extensions.Caching.Memory;

public sealed record CreateMaterialCommand(MaterialModel Material) : ICommand<CreateMaterialCommandResult>;

public sealed record CreateMaterialCommandResult([property:Description("Id материала")]int Id);

public class CreateMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache) : ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult>
{
    public async Task<CreateMaterialCommandResult> Handle(CreateMaterialCommand command, CancellationToken ct)
    {
        var materialName = command.Material.Name.Trim();
        if (await dbContext.Materials.AnyAsync(x => x.Name == materialName, ct))
            throw new BadHttpRequestException("Материал с таким названием уже существует.");
        
        var maxOrderByCol = await dbContext.Materials.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
        
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
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.Materials.AddAsync(material, ct);
        await dbContext.SaveChangesAsync(ct);

        GetMaterialImageQueryResult? imageToCache = null;
        if (command.Material.Image != null)
        {
            var fileFromCache = await dbContext.ImageCache
                .FirstOrDefaultAsync(x => x.Guid == command.Material.Image, ct);
            if(fileFromCache is null)
                throw new BadHttpRequestException("Файл с изображением материала отсутствует в кэше");
            
            await dbContext.MaterialImages.AddAsync(
                new MaterialImage {
                    Data = fileFromCache.Data,
                    Guid = fileFromCache.Guid,
                    MaterialId = material.Id,
                    Type = fileFromCache.Type
                }, ct);

            dbContext.ImageCache.Remove(fileFromCache);
            imageToCache = new GetMaterialImageQueryResult(fileFromCache.Data, fileFromCache.Type);
        }
        
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (command.Material.Image != null && imageToCache != null)
            memoryCache.Set(command.Material.Image.Value, imageToCache);

        return new CreateMaterialCommandResult(material.Id);
    }
}
