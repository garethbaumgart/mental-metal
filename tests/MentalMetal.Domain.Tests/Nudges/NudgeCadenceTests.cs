using MentalMetal.Domain.Common;
using MentalMetal.Domain.Nudges;

namespace MentalMetal.Domain.Tests.Nudges;

public class NudgeCadenceTests
{
    [Fact]
    public void Daily_CalculateFirst_ReturnsFromDate()
    {
        var cadence = NudgeCadence.Daily();
        var today = new DateOnly(2026, 4, 14); // Tuesday
        Assert.Equal(today, cadence.CalculateFirst(today));
    }

    [Fact]
    public void Daily_CalculateNext_ReturnsTomorrow()
    {
        var cadence = NudgeCadence.Daily();
        var today = new DateOnly(2026, 4, 14);
        Assert.Equal(today.AddDays(1), cadence.CalculateNext(today));
    }

    [Fact]
    public void Weekly_CalculateFirst_ReturnsNextTargetDayOrSameDay()
    {
        // 2026-04-14 is a Tuesday. Next Thursday is 2026-04-16.
        var cadence = NudgeCadence.Weekly(DayOfWeek.Thursday);
        Assert.Equal(new DateOnly(2026, 4, 16), cadence.CalculateFirst(new DateOnly(2026, 4, 14)));
    }

    [Fact]
    public void Weekly_CalculateFirst_SameDayOfWeek_ReturnsSameDay()
    {
        var cadence = NudgeCadence.Weekly(DayOfWeek.Thursday);
        var thursday = new DateOnly(2026, 4, 16);
        Assert.Equal(thursday, cadence.CalculateFirst(thursday));
    }

    [Fact]
    public void Weekly_CalculateNext_FromTargetDay_AdvancesOneWeek()
    {
        var cadence = NudgeCadence.Weekly(DayOfWeek.Thursday);
        var thursday = new DateOnly(2026, 4, 16);
        Assert.Equal(thursday.AddDays(7), cadence.CalculateNext(thursday));
    }

    [Fact]
    public void Biweekly_CalculateNext_AdvancesFourteenDays()
    {
        var cadence = NudgeCadence.Biweekly(DayOfWeek.Thursday);
        var thursday = new DateOnly(2026, 4, 16);
        Assert.Equal(thursday.AddDays(14), cadence.CalculateNext(thursday));
    }

    [Fact]
    public void Monthly_CalculateFirst_BeforeDayOfMonth_ReturnsThisMonth()
    {
        var cadence = NudgeCadence.Monthly(15);
        Assert.Equal(new DateOnly(2026, 4, 15), cadence.CalculateFirst(new DateOnly(2026, 4, 10)));
    }

    [Fact]
    public void Monthly_CalculateFirst_AfterDayOfMonth_ReturnsNextMonth()
    {
        var cadence = NudgeCadence.Monthly(15);
        Assert.Equal(new DateOnly(2026, 5, 15), cadence.CalculateFirst(new DateOnly(2026, 4, 20)));
    }

    [Fact]
    public void Monthly_CalculateNext_FromAnchorDay_ReturnsNextMonthAnchor()
    {
        var cadence = NudgeCadence.Monthly(15);
        Assert.Equal(new DateOnly(2026, 5, 15), cadence.CalculateNext(new DateOnly(2026, 4, 15)));
    }

    [Fact]
    public void Monthly_CalculateNext_DayThirtyOne_ClampsToFebruaryEnd()
    {
        var cadence = NudgeCadence.Monthly(31);
        // 2026 is not a leap year => Feb has 28 days.
        Assert.Equal(new DateOnly(2026, 2, 28), cadence.CalculateNext(new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void Monthly_CalculateNext_DayThirtyOne_LeapYearClampsToFebruaryTwentyNinth()
    {
        var cadence = NudgeCadence.Monthly(31);
        // 2024 is a leap year.
        Assert.Equal(new DateOnly(2024, 2, 29), cadence.CalculateNext(new DateOnly(2024, 1, 31)));
    }

    [Fact]
    public void Custom_CalculateNext_AdvancesByInterval()
    {
        var cadence = NudgeCadence.Custom(10);
        var today = new DateOnly(2026, 4, 14);
        Assert.Equal(today.AddDays(10), cadence.CalculateNext(today));
    }

    [Fact]
    public void Custom_ZeroInterval_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => NudgeCadence.Custom(0));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public void Custom_NegativeInterval_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => NudgeCadence.Custom(-1));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public void Custom_ExceedsMax_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => NudgeCadence.Custom(366));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public void Monthly_InvalidDay_Throws()
    {
        Assert.Throws<DomainException>(() => NudgeCadence.Monthly(0));
        Assert.Throws<DomainException>(() => NudgeCadence.Monthly(32));
    }

    [Fact]
    public void FromRequest_WeeklyWithoutDayOfWeek_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            NudgeCadence.FromRequest(CadenceType.Weekly, null, null, null));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public void FromRequest_MonthlyWithoutDayOfMonth_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            NudgeCadence.FromRequest(CadenceType.Monthly, null, null, null));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public void FromRequest_CustomWithoutIntervalDays_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            NudgeCadence.FromRequest(CadenceType.Custom, null, null, null));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }
}
