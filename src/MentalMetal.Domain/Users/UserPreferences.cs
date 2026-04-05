using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Users;

public sealed class UserPreferences : ValueObject
{
    public Theme Theme { get; }
    public bool NotificationsEnabled { get; }
    public TimeOnly BriefingTime { get; }

    private UserPreferences(Theme theme, bool notificationsEnabled, TimeOnly briefingTime)
    {
        Theme = theme;
        NotificationsEnabled = notificationsEnabled;
        BriefingTime = briefingTime;
    }

    public static UserPreferences Default() =>
        new(Theme.Light, notificationsEnabled: true, new TimeOnly(8, 0));

    public static UserPreferences Create(Theme theme, bool notificationsEnabled, TimeOnly briefingTime) =>
        new(theme, notificationsEnabled, briefingTime);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Theme;
        yield return NotificationsEnabled;
        yield return BriefingTime;
    }
}

public enum Theme
{
    Light,
    Dark
}
