using System.ComponentModel;

namespace Catalog.Database.Enums;

public enum PricePercentageTargetEnum
{
    [Description("Материал")]
    MATERIAL = 1,

    [Description("Кромка")]
    EDGE = 2
}