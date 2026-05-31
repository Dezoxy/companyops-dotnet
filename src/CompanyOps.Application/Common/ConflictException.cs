namespace CompanyOps.Application.Common;

/// <summary>
/// Thrown when a use case cannot proceed because it conflicts with existing persisted state —
/// e.g. a unique value (an asset tag) is already taken. The API maps this to a <b>409 Conflict</b>.
/// Distinct from <see cref="CompanyOps.Domain.Common.DomainException"/> (a broken business rule →
/// 400): a conflict is detected against other persisted aggregates, which the pure Domain can't see,
/// so it surfaces at the Application boundary rather than inside an aggregate.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
