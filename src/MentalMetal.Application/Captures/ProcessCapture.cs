using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;
namespace MentalMetal.Application.Captures;

public sealed class ProcessCaptureHandler(
    ICaptureRepository captureRepository,
    IAiCompletionService aiCompletionService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(currentUserService.UserId, captureId);

        capture.BeginProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var request = new AiCompletionRequest(systemPrompt, capture.RawContent, Temperature: 0.2f);
            var result = await aiCompletionService.CompleteAsync(request, cancellationToken);

            var extraction = ParseExtraction(result.Content);
            capture.CompleteProcessing(extraction);
        }
        catch (TasteLimitExceededException)
        {
            capture.FailProcessing("Daily AI limit reached");
        }
        catch (AiProviderException ex)
        {
            capture.FailProcessing(ex.Message);
        }
        catch (System.Text.Json.JsonException ex)
        {
            capture.FailProcessing($"Failed to parse AI response: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            capture.FailProcessing($"Unexpected error: {ex.Message}");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CaptureResponse.From(capture);
    }

    private static string BuildSystemPrompt() => """
        You are an AI assistant that extracts structured data from raw notes, transcripts, and meeting content.
        Analyze the provided content and extract the following in JSON format:

        {
          "summary": "A concise summary of the content (1-3 sentences)",
          "commitments": [
            {
              "description": "What was committed to",
              "direction": "MineToThem",
              "personHint": "Name of the person involved (if mentioned)",
              "dueDate": "Due date if mentioned (ISO format YYYY-MM-DD or null)"
            }
          ],
          "delegations": [
            {
              "description": "What was delegated",
              "personHint": "Name of the person delegated to",
              "dueDate": "Due date if mentioned (ISO format YYYY-MM-DD or null)"
            }
          ],
          "observations": [
            {
              "description": "Notable observation about a person",
              "personHint": "Name of the person observed",
              "tag": "Category tag (e.g., 'strength', 'growth-area', 'feedback', 'career')"
            }
          ],
          "decisions": ["Decision 1", "Decision 2"],
          "risksIdentified": ["Risk 1", "Risk 2"],
          "suggestedPersonLinks": ["Person name 1", "Person name 2"],
          "suggestedInitiativeLinks": ["Initiative name 1"],
          "confidenceScore": 0.85
        }

        Rules:
        - Only extract items explicitly stated or strongly implied in the content.
        - For commitments, set direction to exactly "MineToThem" if the user committed to doing something for someone, or "TheirsToMe" if someone committed to doing something for the user.
        - For delegations, these are tasks the user assigned to someone else.
        - Observations are notable characteristics, behaviors, or feedback about people.
        - The confidence score (0.0-1.0) reflects how confident you are in the overall extraction quality.
        - Return empty arrays for categories with no relevant items.
        - Respond with ONLY the JSON object, no additional text.
        """;

    internal static AiExtraction ParseExtraction(string jsonContent)
    {
        var stripped = JsonResponseParser.StripCodeFences(jsonContent);
        using var json = System.Text.Json.JsonDocument.Parse(stripped);
        var root = json.RootElement;

        return new AiExtraction
        {
            Summary = root.GetProperty("summary").GetString() ?? "",
            Commitments = ParseArray(root, "commitments", e => new ExtractedCommitment
            {
                Description = e.GetProperty("description").GetString() ?? "",
                Direction = ParseDirection(GetOptionalString(e, "direction")),
                PersonHint = GetOptionalString(e, "personHint"),
                DueDate = GetOptionalString(e, "dueDate"),
            }),
            Delegations = ParseArray(root, "delegations", e => new ExtractedDelegation
            {
                Description = e.GetProperty("description").GetString() ?? "",
                PersonHint = GetOptionalString(e, "personHint"),
                DueDate = GetOptionalString(e, "dueDate"),
            }),
            Observations = ParseArray(root, "observations", e => new ExtractedObservation
            {
                Description = e.GetProperty("description").GetString() ?? "",
                PersonHint = GetOptionalString(e, "personHint"),
                Tag = GetOptionalString(e, "tag"),
            }),
            Decisions = ParseStringArray(root, "decisions"),
            RisksIdentified = ParseStringArray(root, "risksIdentified"),
            SuggestedPersonLinks = ParseStringArray(root, "suggestedPersonLinks"),
            SuggestedInitiativeLinks = ParseStringArray(root, "suggestedInitiativeLinks"),
            ConfidenceScore = root.TryGetProperty("confidenceScore", out var cs)
                ? Math.Clamp(cs.GetDecimal(), 0m, 1m)
                : 0m,
        };
    }

    private static List<T> ParseArray<T>(
        System.Text.Json.JsonElement root, string propertyName, Func<System.Text.Json.JsonElement, T> mapper)
    {
        if (!root.TryGetProperty(propertyName, out var arr) || arr.ValueKind != System.Text.Json.JsonValueKind.Array)
            return [];

        return arr.EnumerateArray().Select(mapper).ToList();
    }

    private static List<string> ParseStringArray(System.Text.Json.JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var arr) || arr.ValueKind != System.Text.Json.JsonValueKind.Array)
            return [];

        return arr.EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static ExtractionDirection ParseDirection(string? value) =>
        string.Equals(value, "MineToThem", StringComparison.OrdinalIgnoreCase)
            ? ExtractionDirection.MineToThem
            : ExtractionDirection.TheirsToMe;

    private static string? GetOptionalString(System.Text.Json.JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        return prop.ValueKind == System.Text.Json.JsonValueKind.Null ? null : prop.GetString();
    }
}
