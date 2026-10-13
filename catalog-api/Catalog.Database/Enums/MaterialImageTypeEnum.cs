using System.ComponentModel;

namespace Catalog.Database.Enums;

[Description("Назначение изображения")]
public enum MaterialImageTypeEnum
{
    Original = 1,
    Thumbnail240 = 2,
    Thumbnail480 = 3
}