using MentalMetal.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// Fires the auto-extraction pipeline as a background task outside the HTTP request lifecycle.
/// Creates its own DI scope so the request scope can be disposed immediately.
/// </summary>
public sealed class BackgroundExtractionTrigger(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundExtractionTrigger> logger)
{
    /// <summary>
    /// Fires extraction in the background. Does not await — returns immediately.
    /// Only primitive values are captured to avoid holding request-scoped services.
    /// </summary>
    public void FireAndForget(Guid captureId, Guid userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var backgroundUserScope = scope.ServiceProvider.GetRequiredService<IBackgroundUserScope>();
                backgroundUserScope.SetUserId(userId);

                var handler = scope.ServiceProvider.GetRequiredService<AutoExtractCaptureHandler>();
                var result = await handler.HandleAsync(captureId, CancellationToken.None);

                if (result.ProcessingStatus == Domain.Captures.ProcessingStatus.Failed)
                    logger.LogWarning("Background extraction finished with Failed status for capture {CaptureId}: {Reason}",
                        captureId, result.FailureReason);
                else
                    logger.LogInformation("Background extraction completed for capture {CaptureId}", captureId);
            }
            catch (Exception ex)
            {
                // The handler already marks the capture as Failed internally,
                // so this catch is just for logging unexpected errors (e.g. scope creation failure).
                logger.LogError(ex, "Background extraction failed for capture {CaptureId}", captureId);
            }
        });
    }
}
