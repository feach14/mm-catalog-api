namespace Catalog.Api.Services.Stroydvor;

public sealed record StroydvorPriceSyncOptions
{
    public const string SectionName = "StroydvorPriceSync";

    public bool Enabled { get; init; }
    public string Cron { get; init; } = "0 7-19/4 * * *";
    public string TimeZoneId { get; init; } = "Europe/Moscow";

    internal static bool IsValid(StroydvorPriceSyncOptions options) =>
        !options.Enabled
        || !string.IsNullOrWhiteSpace(options.TimeZoneId)
        && TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZoneId, out _)
        && StroydvorCronSchedule.TryParse(options.Cron, out _);
}