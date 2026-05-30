using System.Net.Http.Json;
using CompanyOps.Application.ExternalSystems;

namespace CompanyOps.Infrastructure.ExternalSystems;

/// <summary>HTTP client for the external Inventory system. See <see cref="FinanceGateway"/>.</summary>
internal sealed class InventoryGateway(HttpClient httpClient) : IInventoryGateway
{
    public async Task<AssetReservation> ReserveAssetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/inventory/reservations",
            new AssetReservationRequest(requestId),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AssetReservationResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Inventory system returned an empty reservation response.");

        return new AssetReservation(body.ReservationId);
    }

    private sealed record AssetReservationRequest(Guid RequestId);
    private sealed record AssetReservationResponse(Guid ReservationId, string Status);
}
