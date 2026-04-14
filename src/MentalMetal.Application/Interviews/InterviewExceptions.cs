namespace MentalMetal.Application.Interviews;

/// <summary>
/// Thrown when the supplied candidate person id does not resolve to a Person owned by
/// the current user, or when that Person's Type is not <c>Candidate</c>. Web layer maps
/// this to HTTP 404 / 400 with error code <c>candidate_not_found</c>.
/// </summary>
public sealed class CandidateNotFoundException(string message) : InvalidOperationException(message);

/// <summary>
/// Thrown when the transcript exceeds the configured max prompt characters. Web layer
/// maps this to HTTP 413.
/// </summary>
public sealed class TranscriptTooLongException(string message) : InvalidOperationException(message);

/// <summary>
/// Thrown when the AI provider fails while generating an interview analysis. Web layer
/// maps this to HTTP 502 with error code <c>analysis_failed</c>.
/// </summary>
public sealed class InterviewAnalysisFailedException(string message, Exception? inner = null)
    : InvalidOperationException(message, inner);
