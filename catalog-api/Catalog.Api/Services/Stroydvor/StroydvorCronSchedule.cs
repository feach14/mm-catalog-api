using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Catalog.Api.Services.Stroydvor;

internal sealed class StroydvorCronSchedule
{
    private readonly int[] _minutes;
    private readonly int[] _hours;
    private readonly bool[] _daysOfMonth;
    private readonly bool[] _months;
    private readonly bool[] _daysOfWeek;
    private readonly bool _anyDayOfMonth;
    private readonly bool _anyDayOfWeek;

    private StroydvorCronSchedule(bool[][] fields, bool anyDayOfMonth, bool anyDayOfWeek)
    {
        _minutes = Enumerable.Range(0, 60).Where(value => fields[0][value]).ToArray();
        _hours = Enumerable.Range(0, 24).Where(value => fields[1][value]).ToArray();
        _daysOfMonth = fields[2];
        _months = fields[3];
        _daysOfWeek = fields[4];
        _anyDayOfMonth = anyDayOfMonth;
        _anyDayOfWeek = anyDayOfWeek;
    }

    public static bool TryParse(string? expression, [NotNullWhen(true)] out StroydvorCronSchedule? schedule)
    {
        schedule = null;
        string[] parts = expression?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length != 5)
            return false;

        int[] minimums = [0, 0, 1, 1, 0];
        int[] maximums = [59, 23, 31, 12, 7];
        bool[][] fields = new bool[5][];
        for (int index = 0; index < fields.Length; index++)
        {
            if (!TryParseField(parts[index], minimums[index], maximums[index], out fields[index]))
                return false;
        }
        fields[4][0] |= fields[4][7];

        // February 2000 includes every calendar date that can occur in any year.
        if (parts[4] == "*" && !Enumerable.Range(1, 12).Any(month =>
                fields[3][month] && Enumerable.Range(1, DateTime.DaysInMonth(2000, month)).Any(day => fields[2][day])))
            return false;

        schedule = new StroydvorCronSchedule(fields, parts[2] == "*", parts[4] == "*");
        return true;
    }

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after, TimeZoneInfo timeZone)
    {
        DateTime date = TimeZoneInfo.ConvertTime(after, timeZone).Date;
        // Eight years also covers the leap-day gap around a non-leap century.
        DateTime lastDate = date.AddYears(Math.Min(8, 9999 - date.Year));
        while (true)
        {
            if (_months[date.Month] && MatchesDay(date))
            {
                DateTimeOffset? earliest = null;
                foreach (int hour in _hours)
                {
                    foreach (int minute in _minutes)
                    {
                        DateTime local = DateTime.SpecifyKind(date.AddHours(hour).AddMinutes(minute), DateTimeKind.Unspecified);
                        if (timeZone.IsInvalidTime(local))
                            continue;

                        TimeSpan[] offsets = timeZone.IsAmbiguousTime(local)
                            ? timeZone.GetAmbiguousTimeOffsets(local)
                            : [timeZone.GetUtcOffset(local)];
                        foreach (TimeSpan offset in offsets)
                        {
                            long utcTicks = local.Ticks - offset.Ticks;
                            if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
                                continue;
                            DateTimeOffset candidate = new(utcTicks, TimeSpan.Zero);
                            if (candidate > after && (earliest is null || candidate < earliest))
                                earliest = candidate;
                        }
                    }
                }
                if (earliest is not null)
                    return earliest;
            }
            if (date == lastDate)
                return null;
            date = date.AddDays(1);
        }
    }

    private bool MatchesDay(DateTime date)
    {
        bool dayOfMonthMatches = _daysOfMonth[date.Day];
        bool dayOfWeekMatches = _daysOfWeek[(int)date.DayOfWeek];
        return _anyDayOfMonth || _anyDayOfWeek
            ? dayOfMonthMatches && dayOfWeekMatches
            : dayOfMonthMatches || dayOfWeekMatches;
    }

    private static bool TryParseField(string expression, int minimum, int maximum, out bool[] values)
    {
        values = new bool[maximum + 1];
        foreach (string item in expression.Split(','))
        {
            string[] stepParts = item.Split('/');
            int step = 1;
            if (stepParts.Length > 2 || stepParts.Length == 2 && (!TryParseNumber(stepParts[1], out step) || step <= 0))
                return false;

            int first = minimum;
            int last = maximum;
            if (stepParts[0] != "*")
            {
                string[] rangeParts = stepParts[0].Split('-');
                if (rangeParts.Length > 2 || !TryParseNumber(rangeParts[0], out first))
                    return false;
                if (rangeParts.Length == 2)
                {
                    if (!TryParseNumber(rangeParts[1], out last))
                        return false;
                }
                else if (stepParts.Length == 1)
                    last = first;
            }
            if (first < minimum || last > maximum || first > last)
                return false;
            for (int value = first; value <= last;)
            {
                values[value] = true;
                if (step > last - value)
                    break;
                value += step;
            }
        }
        return true;
    }

    private static bool TryParseNumber(string value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
}