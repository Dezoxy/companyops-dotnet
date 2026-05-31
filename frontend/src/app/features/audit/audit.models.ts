import { ToneLabel } from '../../shared/status-chip/status-chip';

/** Audit actions, as the API serializes the AuditAction enum (strings). */
export type AuditAction =
  | 'RequestCreated'
  | 'RequestSubmitted'
  | 'RequestApproved'
  | 'RequestRejected'
  | 'RequestFulfilled'
  | 'BudgetCommitted'
  | 'AssetReserved';

/** Raw API shape (AuditLogDto). Mapped to the view model below in the service. */
export interface AuditLogDto {
  readonly id: string;
  readonly occurredAtUtc: string;
  readonly actorId: string;
  readonly action: AuditAction;
  readonly targetType: string;
  readonly targetId: string;
  readonly fromStatus: string | null;
  readonly toStatus: string | null;
}

export const AUDIT_ACTION_META: Record<AuditAction, ToneLabel> = {
  RequestCreated: { label: 'Created', tone: 'neutral' },
  RequestSubmitted: { label: 'Submitted', tone: 'info' },
  RequestApproved: { label: 'Approved', tone: 'success' },
  RequestRejected: { label: 'Rejected', tone: 'danger' },
  RequestFulfilled: { label: 'Fulfilled', tone: 'success' },
  BudgetCommitted: { label: 'Budget committed', tone: 'info' },
  AssetReserved: { label: 'Asset reserved', tone: 'info' },
};

export interface AuditLogVm {
  readonly id: string;
  readonly occurredAt: Date;
  /** Friendly actor — the reserved worker id renders as "System", otherwise the raw id. */
  readonly actorLabel: string;
  readonly action: AuditAction;
  readonly actionMeta: ToneLabel;
  readonly targetType: string;
  readonly targetId: string;
  readonly fromStatus: string | null;
  readonly toStatus: string | null;
}
