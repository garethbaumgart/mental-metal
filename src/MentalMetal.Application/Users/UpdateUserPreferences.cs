using MentalMetal.Application.Common;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class UpdateUserPreferencesHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        if (!Enum.TryParse<Theme>(request.Theme, ignoreCase: true, out var theme))
            throw new ArgumentException($"'{request.Theme}' is not a valid theme.", nameof(request));

        var preferences = UserPreferences.Create(theme, request.NotificationsEnabled, request.BriefingTime, request.LivingBriefAutoApply);
        user.UpdatePreferences(preferences);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
