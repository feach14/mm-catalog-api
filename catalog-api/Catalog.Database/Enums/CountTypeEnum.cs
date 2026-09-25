namespace Catalog.Database.Enums;

public enum CountTypeEnum : int
{
    M2 = 1, // метр квадратный
    PM = 2, // погонный метр
    SHT = 3, // шт.
    PERCENT = 4, // %
    LIST = 5, // лист
    PACK = 6, // пакет, упаковка, пачка, комплект
    FIX = 7, // Фикс. Служит для применения ко всему расчету один раз
    KG = 8 // Килограмм
}