namespace CompanyOps.Infrastructure.ExternalSystems;

/// <summary>Base URLs for the external (mock) systems, bound from "ExternalSystems".</summary>
public sealed class ExternalSystemsOptions
{
    public const string SectionName = "ExternalSystems";

    public string FinanceBaseUrl { get; init; } = "";
    public string InventoryBaseUrl { get; init; } = "";
}
