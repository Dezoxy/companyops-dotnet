namespace CompanyOps.Application.ExternalSystems;

/// <summary>Result of reserving an asset in the external Inventory system.</summary>
public sealed record AssetReservation(Guid ReservationId);

/// <summary>
/// Port to the external Inventory system. Called (from the Worker) when a request is
/// fulfilled, to reserve the asset. Throws if the call fails after the resilience
/// pipeline is exhausted (ADR 0008).
/// </summary>
public interface IInventoryGateway
{
    Task<AssetReservation> ReserveAssetAsync(Guid requestId, CancellationToken cancellationToken = default);
}
