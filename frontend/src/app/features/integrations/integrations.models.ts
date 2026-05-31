import { ToneLabel } from '../../shared/status-chip/status-chip';

// --- Wire shapes -------------------------------------------------------------
// Mirror the server contracts (IntegrationStatusDto / OutboxSummaryDto / IntegrationMessageDto).
// `status` is a closed set derived server-side (IntegrationStatusStore.StatusOf).

export type IntegrationStatus = 'Pending' | 'Published' | 'Failed';

export interface OutboxSummaryDto {
  readonly total: number;
  readonly pending: number;
  readonly published: number;
  readonly failed: number;
}

export interface IntegrationMessageDto {
  readonly id: string;
  readonly type: string;
  readonly status: IntegrationStatus;
  readonly occurredAtUtc: string;
  readonly processedAtUtc: string | null;
  readonly attempts: number;
  readonly error: string | null;
}

export interface IntegrationStatusDto {
  readonly outbox: OutboxSummaryDto;
  readonly processedByWorker: number;
  readonly recent: readonly IntegrationMessageDto[];
}

// --- View models -------------------------------------------------------------

export interface StatTile {
  readonly label: string;
  readonly value: number;
  readonly icon: string;
}

export interface IntegrationMessageVm {
  readonly id: string;
  readonly type: string;
  readonly statusMeta: ToneLabel;
  readonly occurredAt: Date;
  readonly attempts: number;
  readonly error: string | null;
}

export interface IntegrationStatusVm {
  readonly tiles: readonly StatTile[];
  readonly messages: readonly IntegrationMessageVm[];
}

// status → chip tone. A closed set derived by the API, but the wire value is a string at runtime;
// an unrecognised status degrades to a neutral chip rather than throwing and taking down the table.
const STATUS_META: Record<IntegrationStatus, ToneLabel> = {
  Pending: { label: 'Pending', tone: 'info' },
  Published: { label: 'Published', tone: 'success' },
  Failed: { label: 'Failed', tone: 'danger' },
};

function statusMeta(status: string): ToneLabel {
  const meta: ToneLabel | undefined = STATUS_META[status as IntegrationStatus];
  return meta ?? { label: status, tone: 'neutral' };
}

export function mapIntegrationStatus(dto: IntegrationStatusDto): IntegrationStatusVm {
  return {
    tiles: [
      { label: 'Total', value: dto.outbox.total, icon: 'all_inbox' },
      { label: 'Pending', value: dto.outbox.pending, icon: 'schedule' },
      { label: 'Published', value: dto.outbox.published, icon: 'cloud_done' },
      { label: 'Failed', value: dto.outbox.failed, icon: 'error_outline' },
      { label: 'Processed by worker', value: dto.processedByWorker, icon: 'done_all' },
    ],
    messages: dto.recent.map((message) => ({
      id: message.id,
      type: message.type,
      statusMeta: statusMeta(message.status),
      occurredAt: new Date(message.occurredAtUtc),
      attempts: message.attempts,
      error: message.error,
    })),
  };
}
