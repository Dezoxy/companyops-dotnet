import { ToneLabel } from '../../shared/status-chip/status-chip';

// --- Wire enums (serialized as strings by the API) ---------------------------
export type AssetStatus = 'InStock' | 'Assigned' | 'InRepair' | 'Retired';
export type AssetType = 'Laptop' | 'Desktop' | 'Mobile' | 'Monitor' | 'Peripheral' | 'Software' | 'Other';

// --- Raw API DTOs ------------------------------------------------------------
export interface AssetDto {
  readonly id: string;
  readonly tag: string;
  readonly name: string;
  readonly type: AssetType;
  readonly status: AssetStatus;
  readonly assignedToId: string | null;
  readonly createdAtUtc: string;
}

export interface AssetHistoryDto {
  readonly id: string;
  readonly occurredAtUtc: string;
  readonly actorId: string;
  readonly action: string;
  readonly fromStatus: string | null;
  readonly toStatus: string | null;
}

// --- Display metadata --------------------------------------------------------
export const ASSET_STATUS_META: Record<AssetStatus, ToneLabel> = {
  InStock: { label: 'In stock', tone: 'info' },
  Assigned: { label: 'Assigned', tone: 'success' },
  InRepair: { label: 'In repair', tone: 'progress' },
  Retired: { label: 'Retired', tone: 'neutral' },
};

export const ASSET_TYPE_LABEL: Record<AssetType, string> = {
  Laptop: 'Laptop',
  Desktop: 'Desktop',
  Mobile: 'Mobile',
  Monitor: 'Monitor',
  Peripheral: 'Peripheral',
  Software: 'Software',
  Other: 'Other',
};

// Material Symbol per asset type — shown beside the type in the inventory table.
export const ASSET_TYPE_ICON: Record<AssetType, string> = {
  Laptop: 'laptop_mac',
  Desktop: 'desktop_windows',
  Mobile: 'smartphone',
  Monitor: 'monitor',
  Peripheral: 'keyboard',
  Software: 'apps',
  Other: 'devices_other',
};

/** Friendly labels for the asset audit actions shown in the history. */
export const ASSET_ACTION_LABEL: Record<string, string> = {
  AssetRegistered: 'Registered',
  AssetAssigned: 'Assigned',
  AssetReclaimed: 'Reclaimed',
  AssetSentToRepair: 'Sent to repair',
  AssetReturnedFromRepair: 'Returned from repair',
  AssetRetired: 'Retired',
};

// --- View models -------------------------------------------------------------
export interface AssetVm {
  readonly id: string;
  readonly tag: string;
  readonly name: string;
  readonly type: AssetType;
  readonly typeLabel: string;
  readonly status: AssetStatus;
  readonly statusMeta: ToneLabel;
  readonly assignedToId: string | null;
  /** Short display form of the assignee id for the table (first 8 chars, upper-cased); null when
   *  unassigned. Derived in the mapper so it isn't recomputed in templates. */
  readonly assignedToIdShort: string | null;
  readonly createdAt: Date;
}

export interface AssetHistoryVm {
  readonly id: string;
  readonly occurredAt: Date;
  readonly actorId: string;
  readonly actionLabel: string;
  readonly fromStatus: string | null;
  readonly toStatus: string | null;
}

/** Body for POST /assets. */
export interface RegisterAssetInput {
  readonly tag: string;
  readonly name: string;
  readonly type: AssetType;
}
