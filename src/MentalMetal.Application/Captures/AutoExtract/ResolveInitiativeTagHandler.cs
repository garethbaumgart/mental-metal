using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.AutoExtract;

public sealed record ResolveInitiativeTagRequest(string RawName, Guid InitiativeId);

public sealed class ResolveInitiativeTagHandler(
    ICaptureRepository captureRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(
        Guid captureId, ResolveInitiativeTagRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(userId, captureId);

        if (capture.AiExtraction is null)
            throw new InvalidOperationException("Capture has no AI extraction to resolve.");

        // Validate that rawName matches an existing InitiativeTag entry
        var tagExists = capture.AiExtraction.InitiativeTags
            .Any(t => string.Equals(t.RawName, request.RawName, StringComparison.OrdinalIgnoreCase));
        if (!tagExists)
            throw new InvalidOperationException(
                $"No initiative tag with raw name '{request.RawName}' found in extraction.");

        var initiative = await initiativeRepository.GetByIdAsync(request.InitiativeId, cancellationToken)
            ?? throw new InvalidOperationException($"Initiative not found: {request.InitiativeId}");

        if (initiative.UserId != userId)
            throw new InvalidOperationException($"Initiative not found: {request.InitiativeId}");

        // Update the extraction with resolved InitiativeId
        var updatedTags = capture.AiExtraction.InitiativeTags.Select(t =>
            string.Equals(t.RawName, request.RawName, StringComparison.OrdinalIgnoreCase)
                ? t with { InitiativeId = request.InitiativeId }
                : t).ToList();

        var updatedExtraction = capture.AiExtraction with
        {
            InitiativeTags = updatedTags
        };

        capture.UpdateExtraction(updatedExtraction);

        // Link capture to the initiative
        capture.LinkToInitiative(request.InitiativeId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
