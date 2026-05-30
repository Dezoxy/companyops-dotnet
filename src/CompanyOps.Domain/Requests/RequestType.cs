namespace CompanyOps.Domain.Requests;

/// <summary>
/// The kind of request. Per ADR 0005 this is the key that selects the approval
/// chain and fulfillment action, so the same engine serves multiple internal
/// processes. In Phase 1 it is only stored; Phase 2 makes it drive the workflow.
/// </summary>
public enum RequestType
{
    /// <summary>Procurement / asset purchase — the seed flow (manager → finance → IT).</summary>
    Procurement = 0,

    /// <summary>IT request / helpdesk-light — service or access request (Phase 13).</summary>
    Helpdesk = 1,

    /// <summary>Asset lifecycle action — assign, return, repair, retire (Phase 14).</summary>
    AssetLifecycle = 2,
}
