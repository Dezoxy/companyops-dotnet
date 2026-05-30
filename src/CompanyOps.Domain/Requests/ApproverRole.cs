namespace CompanyOps.Domain.Requests;

/// <summary>
/// The role that can satisfy an approval step. Distinct from the broader app role
/// model arriving in Phase 3 — this is specifically "who is allowed to decide this
/// step". An approval chain (see <see cref="ApprovalChains"/>) is a sequence of
/// steps each keyed to one of these.
/// </summary>
public enum ApproverRole
{
    Manager = 0,
    Finance = 1,
    ItAdmin = 2,
}
