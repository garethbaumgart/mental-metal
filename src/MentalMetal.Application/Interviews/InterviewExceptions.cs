namespace MentalMetal.Application.Interviews;

/// <summary>
/// Thrown when the supplied candidate person id does not resolve to a Person owned by
/// the current user (i.e. the record does not exist or belongs to another user).
/// Web layer maps this to HTTP 404 with error code <c>candidate_not_found</c>.
/// </summary>
public sealed class CandidateNotFoundException(string message) : InvalidOperationException(message);

/// <summary>
/// Thrown when the supplied candidate person id resolves to a Person owned by the
/// current user but whose <c>Type</c> is not <c>Candidate</c>. Web layer maps this to
/// HTTP 400 with error code <c>candidate_wrong_type</c>.
/// </summary>
public sealed class CandidateWrongTypeException(string message) : InvalidOperationException(message);

/// <summary>
/// Thrown when a scorecard id supplied to an update/remove operation does not exist
/// on the interview aggregate. Web layer maps this to HTTP 404 with error code
/// <c>scorecard_not_found</c>.
/// </summary>
public sealed class ScorecardNotFoundException(string message) : InvalidOperationException(message);

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
