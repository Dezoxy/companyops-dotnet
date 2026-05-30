namespace CompanyOps.Domain.Common;

/// <summary>
/// Thrown when a domain rule or invariant is violated. The API layer maps this to
/// a 4xx response. Business rules live here in the Domain — they throw, they are
/// not merely guarded in the UI/API.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
