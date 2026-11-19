using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Database.Search;

public static class MaterialSearchTextHelper
{
    public static async Task UpdateAsync(CatalogDbContext dbContext, IEnumerable<Material> materials, CancellationToken ct)
    {
        foreach (var material in materials)
        {
            var materialType = await dbContext.MaterialTypes.FindAsync([material.MaterialTypeId], ct);
            var manufacturer = await dbContext.MaterialManufacturers.FindAsync([material.MaterialManufacturerId], ct);
            var thickness = await dbContext.MaterialThicknesses.FindAsync([material.MaterialThicknessId], ct);
            var sheetSize = await dbContext.MaterialSheetSizes.FindAsync([material.MaterialSheetSizeId], ct);
            material.SearchText = MaterialSearchText.Create(material, materialType!, manufacturer!, thickness!, sheetSize!);
        }
    }

    public static async Task UpdateAsync(CatalogDbContext dbContext, IQueryable<Material> materials, CancellationToken ct)
    {
        var items = await WithReferences(materials).ToArrayAsync(ct);
        await UpdateAsync(dbContext, items, ct);
    }

    public static async Task<int> BackfillAsync(CatalogDbContext dbContext, CancellationToken ct, bool rebuild = false)
    {
        var count = 0;
        var lastId = 0;
        while (true)
        {
            var materials = await WithReferences(dbContext.Materials.Where(material => material.Id > lastId
                    && (rebuild || material.SearchText == string.Empty)))
                .OrderBy(material => material.Id).Take(500).ToArrayAsync(ct);
            if (materials.Length == 0)
                return count;

            await UpdateAsync(dbContext, materials, ct);
            await dbContext.SaveChangesAsync(ct);
            count += materials.Length;
            lastId = materials[^1].Id;
            dbContext.ChangeTracker.Clear();
        }
    }

    private static IQueryable<Material> WithReferences(IQueryable<Material> materials) => materials
        .Include(material => material.MaterialType)
        .Include(material => material.MaterialManufacturer)
        .Include(material => material.MaterialThickness)
        .Include(material => material.MaterialSheetSize);
}