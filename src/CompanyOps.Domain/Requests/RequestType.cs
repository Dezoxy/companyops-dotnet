namespace CompanyOps.Domain.Requests;

/// <summary>
/// The kind of request. Per ADR 0005 this is the key that selects the approval
/// chain and fulfillment action, so the same engine serves multiple internal
/// processes.
/// </summary>
public enum RequestType
{
    /// <summary>Procurement / asset purchase — the seed flow (manager → finance → IT).</summary>
    Procurement = 0,

    /// <summary>IT request / helpdesk-light — service or access request (Phase 15).</summary>
    Helpdesk = 1,

    /// <summary>Asset lifecycle action — assign an asset to the requester (Phase 16).</summary>
    AssetLifecycle = 2,
}
