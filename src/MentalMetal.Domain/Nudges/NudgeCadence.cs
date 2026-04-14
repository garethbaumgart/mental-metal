using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Nudges;

/// <summary>
/// Value object describing how a <see cref="Nudge"/> recurs. Pure arithmetic;
/// all methods take the reference date explicitly for determinism.
/// </summary>
public sealed class NudgeCadence : ValueObject
{
    public const int MaxCustomIntervalDays = 365;

    public CadenceType Type { get; }
    public int? CustomIntervalDays { get; }
    public DayOfWeek? DayOfWeek { get; }
    public int? DayOfMonth { get; }

    private NudgeCadence(CadenceType type, int? customIntervalDays, DayOfWeek? dayOfWeek, int? dayOfMonth)
    {
        Type = type;
        CustomIntervalDays = customIntervalDays;
        DayOfWeek = dayOfWeek;
        DayOfMonth = dayOfMonth;
    }

    public static NudgeCadence Daily() => new(CadenceType.Daily, null, null, null);

    public static NudgeCadence Weekly(DayOfWeek dayOfWeek) =>
        new(CadenceType.Weekly, null, dayOfWeek, null);

    public static NudgeCadence Biweekly(DayOfWeek dayOfWeek) =>
        new(CadenceType.Biweekly, null, dayOfWeek, null);

    public static NudgeCadence Monthly(int dayOfMonth)
    {
        if (dayOfMonth is < 1 or > 31)
            throw new DomainException("DayOfMonth must be between 1 and 31.", "nudge.invalidCadence");
        return new NudgeCadence(CadenceType.Monthly, null, null, dayOfMonth);
    }

    public static NudgeCadence Custom(int customIntervalDays)
    {
        if (customIntervalDays is < 1 or > MaxCustomIntervalDays)
            throw new DomainException(
                $"CustomIntervalDays must be between 1 and {MaxCustomIntervalDays}.", "nudge.invalidCadence");
        return new NudgeCadence(CadenceType.Custom, customIntervalDays, null, null);
    }

    /// <summary>
    /// Factory matching the request-shape of create/update handlers. Enforces cadence-specific required fields.
    /// </summary>
    public static NudgeCadence FromRequest(
        CadenceType type,
        int? customIntervalDays,
        DayOfWeek? dayOfWeek,
        int? dayOfMonth)
    {
        return type switch
        {
            CadenceType.Daily => Daily(),
            CadenceType.Weekly => dayOfWeek is not null
                ? Weekly(dayOfWeek.Value)
                : throw new DomainException("Weekly cadence requires DayOfWeek.", "nudge.invalidCadence"),
            CadenceType.Biweekly => dayOfWeek is not null
                ? Biweekly(dayOfWeek.Value)
                : throw new DomainException("Biweekly cadence requires DayOfWeek.", "nudge.invalidCadence"),
            CadenceType.Monthly => dayOfMonth is not null
                ? Monthly(dayOfMonth.Value)
                : throw new DomainException("Monthly cadence requires DayOfMonth.", "nudge.invalidCadence"),
            CadenceType.Custom => customIntervalDays is not null
                ? Custom(customIntervalDays.Value)
                : throw new DomainException("Custom cadence requires CustomIntervalDays.", "nudge.invalidCadence"),
            _ => throw new DomainException("Unknown cadence type.", "nudge.invalidCadence"),
        };
    }

    /// <summary>
    /// First scheduled occurrence on or after <paramref name="from"/>.
    /// Used by Create and Resume to anchor the schedule.
    /// </summary>
    public DateOnly CalculateFirst(DateOnly from) => Type switch
    {
        CadenceType.Daily => from,
        CadenceType.Weekly => NextOrSameDayOfWeek(from, DayOfWeek!.Value),
        CadenceType.Biweekly => NextOrSameDayOfWeek(from, DayOfWeek!.Value),
        CadenceType.Monthly => NextOrSameDayOfMonth(from, DayOfMonth!.Value),
        CadenceType.Custom => from,
        _ => throw new InvalidOperationException("Unknown cadence type."),
    };

    /// <summary>
    /// Next scheduled occurrence strictly after <paramref name="after"/>.
    /// Used by MarkNudged so the schedule always advances past the moment of marking.
    /// </summary>
    public DateOnly CalculateNext(DateOnly after) => Type switch
    {
        CadenceType.Daily => after.AddDays(1),
        CadenceType.Weekly => NextOrSameDayOfWeek(after.AddDays(1), DayOfWeek!.Value),
        CadenceType.Biweekly => NextOrSameDayOfWeek(after.AddDays(14), DayOfWeek!.Value),
        CadenceType.Monthly => NextDayOfMonthStrictlyAfter(after, DayOfMonth!.Value),
        CadenceType.Custom => after.AddDays(CustomIntervalDays!.Value),
        _ => throw new InvalidOperationException("Unknown cadence type."),
    };

    private static DateOnly NextOrSameDayOfWeek(DateOnly from, DayOfWeek target)
    {
        var diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(diff);
    }

    private static DateOnly NextOrSameDayOfMonth(DateOnly from, int dayOfMonth)
    {
        var candidate = ClampToMonth(from.Year, from.Month, dayOfMonth);
        if (candidate >= from)
            return candidate;

        var nextMonth = from.AddMonths(1);
        return ClampToMonth(nextMonth.Year, nextMonth.Month, dayOfMonth);
    }

    private static DateOnly NextDayOfMonthStrictlyAfter(DateOnly after, int dayOfMonth)
    {
        var candidate = ClampToMonth(after.Year, after.Month, dayOfMonth);
        if (candidate > after)
            return candidate;

        var nextMonth = after.AddMonths(1);
        return ClampToMonth(nextMonth.Year, nextMonth.Month, dayOfMonth);
    }

    private static DateOnly ClampToMonth(int year, int month, int dayOfMonth)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        var day = dayOfMonth > lastDay ? lastDay : dayOfMonth;
        return new DateOnly(year, month, day);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return CustomIntervalDays;
        yield return DayOfWeek;
        yield return DayOfMonth;
    }
}
