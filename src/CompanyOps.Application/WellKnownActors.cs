namespace CompanyOps.Application;

/// <summary>
/// Reserved actor ids that do not correspond to a human principal (Keycloak <c>sub</c>).
/// </summary>
public static class WellKnownActors
{
    /// <summary>
    /// The Worker/system actor recorded on audit entries for actions performed by the
    /// system itself (e.g. committing budget / reserving an asset after an event), where
    /// there is no authenticated user. Reserved — never assign it to a real user.
    /// </summary>
    public static readonly Guid SystemWorker = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
}
