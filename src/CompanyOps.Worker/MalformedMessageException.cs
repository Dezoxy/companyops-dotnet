namespace CompanyOps.Worker;

/// <summary>
/// Thrown for an unprocessable message (unknown type, bad payload). The consumer
/// dead-letters these immediately rather than retrying a permanent failure.
/// </summary>
public sealed class MalformedMessageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
