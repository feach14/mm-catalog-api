using Catalog.Database.AppConstants;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class PricePercentageConfiguration : IEntityTypeConfiguration<PricePercentage>
{
    public void Configure(EntityTypeBuilder<PricePercentage> eb)
    {
        eb.HasKey(percentage => percentage.Id);
        eb.Property(percentage => percentage.Key).IsRequired().HasMaxLength(100);
        eb.Property(percentage => percentage.Name).IsRequired().HasMaxLength(250);
        eb.Property(percentage => percentage.Description).IsRequired();
        eb.Property(percentage => percentage.AppliesTo).HasConversion<string>().IsRequired();
        eb.Property(percentage => percentage.PriceType).HasConversion<string>().IsRequired();
        eb.HasIndex(percentage => percentage.Key).IsUnique();
        eb.HasData(
            new
            {
                Id = 1,
                Key = $"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.CUSTOM}",
                Name = "Надбавка на материал для частного клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.MATERIAL,
                PriceType = PriceTypeEnum.CUSTOM,
                Description = "Увеличивает базовую стоимость листового материала для частного клиента на указанный процент."
            },
            new
            {
                Id = 2,
                Key = $"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.MEBEL}",
                Name = "Надбавка на материал для оптового клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.MATERIAL,
                PriceType = PriceTypeEnum.MEBEL,
                Description = "Увеличивает базовую стоимость листового материала для оптового клиента на указанный процент."
            },
            new
            {
                Id = 3,
                Key = $"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.KEY}",
                Name = "Надбавка на материал для индивидуального клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.MATERIAL,
                PriceType = PriceTypeEnum.KEY,
                Description = "Увеличивает базовую стоимость листового материала для индивидуального клиента на указанный процент."
            },
            new
            {
                Id = 7,
                Key = $"{PricePercentageConstants.Edge04KeyPrefix}_{PriceTypeEnum.CUSTOM}",
                Name = "Надбавка на кромку 0,4 мм для частного клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.CUSTOM,
                Description = "Увеличивает базовую стоимость кромки толщиной 0,4 мм для частного клиента на указанный процент."
            },
            new
            {
                Id = 8,
                Key = $"{PricePercentageConstants.Edge04KeyPrefix}_{PriceTypeEnum.MEBEL}",
                Name = "Надбавка на кромку 0,4 мм для оптового клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.MEBEL,
                Description = "Увеличивает базовую стоимость кромки толщиной 0,4 мм для оптового клиента на указанный процент."
            },
            new
            {
                Id = 9,
                Key = $"{PricePercentageConstants.Edge04KeyPrefix}_{PriceTypeEnum.KEY}",
                Name = "Надбавка на кромку 0,4 мм для индивидуального клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.KEY,
                Description = "Увеличивает базовую стоимость кромки толщиной 0,4 мм для индивидуального клиента на указанный процент."
            },
            new
            {
                Id = 10,
                Key = $"{PricePercentageConstants.Edge1KeyPrefix}_{PriceTypeEnum.CUSTOM}",
                Name = "Надбавка на кромку 1 мм для частного клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.CUSTOM,
                Description = "Увеличивает базовую стоимость кромки толщиной 1 мм для частного клиента на указанный процент."
            },
            new
            {
                Id = 11,
                Key = $"{PricePercentageConstants.Edge1KeyPrefix}_{PriceTypeEnum.MEBEL}",
                Name = "Надбавка на кромку 1 мм для оптового клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.MEBEL,
                Description = "Увеличивает базовую стоимость кромки толщиной 1 мм для оптового клиента на указанный процент."
            },
            new
            {
                Id = 12,
                Key = $"{PricePercentageConstants.Edge1KeyPrefix}_{PriceTypeEnum.KEY}",
                Name = "Надбавка на кромку 1 мм для индивидуального клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.KEY,
                Description = "Увеличивает базовую стоимость кромки толщиной 1 мм для индивидуального клиента на указанный процент."
            },
            new
            {
                Id = 13,
                Key = $"{PricePercentageConstants.Edge2KeyPrefix}_{PriceTypeEnum.CUSTOM}",
                Name = "Надбавка на кромку 2 мм для частного клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.CUSTOM,
                Description = "Увеличивает базовую стоимость кромки толщиной 2 мм для частного клиента на указанный процент."
            },
            new
            {
                Id = 14,
                Key = $"{PricePercentageConstants.Edge2KeyPrefix}_{PriceTypeEnum.MEBEL}",
                Name = "Надбавка на кромку 2 мм для оптового клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.MEBEL,
                Description = "Увеличивает базовую стоимость кромки толщиной 2 мм для оптового клиента на указанный процент."
            },
            new
            {
                Id = 15,
                Key = $"{PricePercentageConstants.Edge2KeyPrefix}_{PriceTypeEnum.KEY}",
                Name = "Надбавка на кромку 2 мм для индивидуального клиента",
                Value = 0,
                AppliesTo = PricePercentageTargetEnum.EDGE,
                PriceType = PriceTypeEnum.KEY,
                Description = "Увеличивает базовую стоимость кромки толщиной 2 мм для индивидуального клиента на указанный процент."
            });
    }
}