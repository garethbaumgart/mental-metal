using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Users;

public sealed class UserPreferences : ValueObject
{
    public Theme Theme { get; }
    public bool NotificationsEnabled { get; }
    public TimeOnly BriefingTime { get; }
    public bool LivingBriefAutoApply { get; }

    private UserPreferences(Theme theme, bool notificationsEnabled, TimeOnly briefingTime, bool livingBriefAutoApply)
    {
        Theme = theme;
        NotificationsEnabled = notificationsEnabled;
        BriefingTime = briefingTime;
        LivingBriefAutoApply = livingBriefAutoApply;
    }

    public static UserPreferences Default() =>
        new(Theme.Light, notificationsEnabled: true, new TimeOnly(8, 0), livingBriefAutoApply: false);

    public static UserPreferences Create(Theme theme, bool notificationsEnabled, TimeOnly briefingTime, bool livingBriefAutoApply = false) =>
        new(theme, notificationsEnabled, briefingTime, livingBriefAutoApply);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Theme;
        yield return NotificationsEnabled;
        yield return BriefingTime;
        yield return LivingBriefAutoApply;
    }
}

public enum Theme
{
    Light,
    Dark
}
