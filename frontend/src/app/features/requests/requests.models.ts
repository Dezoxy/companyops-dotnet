import { ToneLabel } from '../../shared/status-chip/status-chip';

// --- Wire enums --------------------------------------------------------------
// The API serializes enums as strings (JsonStringEnumConverter), so these mirror the
// domain enum names exactly. See src/CompanyOps.Domain/Requests.

export type RequestType = 'Procurement' | 'Helpdesk' | 'AssetLifecycle';

export type RequestStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'InFulfillment'
  | 'Completed'
  | 'Rejected'
  | 'Cancelled';

export type ApproverRole = 'Manager' | 'Finance' | 'ItAdmin';
export type ApprovalScope = 'Department' | 'Global';
export type ApprovalDecision = 'Pending' | 'Approved' | 'Rejected';

// --- Raw API DTOs ------------------------------------------------------------
// Mirror the server contracts (RequestDto / ApprovalStepDto). Mapped to the view
// models below in the service — components never see these shapes.

export interface ApprovalStepDto {
  readonly order: number;
  readonly requiredRole: ApproverRole;
  readonly scope: ApprovalScope;
  readonly isRequired: boolean;
  readonly decision: ApprovalDecision;
  readonly decidedById: string | null;
  readonly decidedAtUtc: string | null;
  readonly note: string | null;
}

export interface RequestDto {
  readonly id: string;
  readonly title: string;
  readonly description: string | null;
  readonly type: RequestType;
  readonly status: RequestStatus;
  readonly requesterId: string;
  readonly departmentId: string;
  readonly createdAtUtc: string;
  readonly approvalSteps: readonly ApprovalStepDto[];
}

/** Body for POST /requests (create). Requester + department come from the JWT, never the body. */
export interface CreateRequestInput {
  readonly title: string;
  readonly description?: string | null;
  readonly type: RequestType;
}

// --- Display metadata --------------------------------------------------------
// Label + chip tone per enum value. Single source so the list, detail, and dashboard
// render a status identically.

export const REQUEST_STATUS_META: Record<RequestStatus, ToneLabel> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Submitted: { label: 'Submitted', tone: 'info' },
  Approved: { label: 'Approved', tone: 'success' },
  InFulfillment: { label: 'In fulfilment', tone: 'progress' },
  Completed: { label: 'Completed', tone: 'success' },
  Rejected: { label: 'Rejected', tone: 'danger' },
  Cancelled: { label: 'Cancelled', tone: 'neutral' },
};

export const APPROVAL_DECISION_META: Record<ApprovalDecision, ToneLabel> = {
  Pending: { label: 'Pending', tone: 'neutral' },
  Approved: { label: 'Approved', tone: 'success' },
  Rejected: { label: 'Rejected', tone: 'danger' },
};

export const REQUEST_TYPE_LABEL: Record<RequestType, string> = {
  Procurement: 'Procurement',
  Helpdesk: 'Helpdesk',
  AssetLifecycle: 'Asset lifecycle',
};

export const APPROVER_ROLE_LABEL: Record<ApproverRole, string> = {
  Manager: 'Manager',
  Finance: 'Finance',
  ItAdmin: 'IT Admin',
};

// --- View models -------------------------------------------------------------
// What components consume. Derived display fields (labels, tones, parsed dates) are
// computed once in the service mapper, not in templates.

export interface ApprovalStepVm {
  readonly order: number;
  readonly requiredRole: ApproverRole;
  readonly roleLabel: string;
  readonly scope: ApprovalScope;
  readonly isRequired: boolean;
  readonly decision: ApprovalDecision;
  readonly decisionMeta: ToneLabel;
  readonly decidedById: string | null;
  readonly decidedAt: Date | null;
  readonly note: string | null;
  /** True for the first still-pending step — the one the chain is waiting on. */
  readonly isCurrent: boolean;
}

export interface RequestVm {
  readonly id: string;
  readonly title: string;
  readonly description: string | null;
  readonly type: RequestType;
  readonly typeLabel: string;
  readonly status: RequestStatus;
  readonly statusMeta: ToneLabel;
  readonly requesterId: string;
  readonly departmentId: string;
  readonly createdAt: Date;
  readonly approvalSteps: readonly ApprovalStepVm[];
}
