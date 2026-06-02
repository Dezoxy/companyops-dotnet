import { Tone, ToneLabel } from '../../shared/status-chip/status-chip';
import {
  REQUEST_PRIORITY_META,
  REQUEST_STATUS_META,
  REQUEST_TYPE_LABEL,
  RequestPriority,
  RequestStatus,
  RequestType,
} from '../requests/requests.models';
import { ASSET_STATUS_META, ASSET_TYPE_LABEL, AssetStatus, AssetType } from '../assets/assets.models';

// --- Raw API DTOs ------------------------------------------------------------
// Mirror the server contracts (CategoryCount / RequestReportDto / AssetReportDto). The server
// aggregates by GROUP BY and returns enum-name keys; the metadata below turns them into labels
// and tones — the report itself is presentation-agnostic.

export interface CategoryCountDto {
  readonly key: string;
  readonly count: number;
}

export interface RequestReportDto {
  readonly total: number;
  readonly byStatus: readonly CategoryCountDto[];
  readonly byType: readonly CategoryCountDto[];
  readonly byPriority: readonly CategoryCountDto[];
}

export interface AssetReportDto {
  readonly total: number;
  readonly byStatus: readonly CategoryCountDto[];
  readonly byType: readonly CategoryCountDto[];
}

// --- KPI counts view model ---------------------------------------------------
// The request aggregates keyed by enum name for direct, type-safe lookup (e.g. the dashboard KPI
// cards). Maps the GROUP BY arrays into records so callers read `byStatus.Submitted` instead of
// scanning a DTO array — no raw HTTP shape leaks past the service.

export interface KpiCounts {
  readonly total: number;
  readonly byStatus: Readonly<Partial<Record<RequestStatus, number>>>;
  readonly byPriority: Readonly<Partial<Record<RequestPriority, number>>>;
}

function toCountMap<K extends string>(buckets: readonly CategoryCountDto[]): Partial<Record<K, number>> {
  return Object.fromEntries(buckets.map((b) => [b.key, b.count])) as Partial<Record<K, number>>;
}

export function mapKpiCounts(dto: RequestReportDto): KpiCounts {
  return {
    total: dto.total,
    byStatus: toCountMap<RequestStatus>(dto.byStatus),
    byPriority: toCountMap<RequestPriority>(dto.byPriority),
  };
}

// --- View models -------------------------------------------------------------

/** One bar in a breakdown: its label, count, share of the section total (bar width), and tone. */
export interface BreakdownRow {
  readonly label: string;
  readonly count: number;
  readonly percent: number;
  readonly tone: Tone;
}

export interface BreakdownSection {
  readonly title: string;
  readonly rows: readonly BreakdownRow[];
}

export interface ReportVm {
  readonly title: string;
  readonly total: number;
  readonly sections: readonly BreakdownSection[];
}

// Type buckets have no inherent status tone — render them in a neutral accent.
const TYPE_TONE: Tone = 'info';

// Defensive lookups: a bucket key the client doesn't recognise (e.g. a server enum value added
// ahead of the client) degrades to a neutral row instead of throwing — a report iterates every
// value the database holds, so one unknown key must not break the whole screen.
function withTone(meta: ToneLabel | undefined, key: string): ToneLabel {
  return meta ?? { label: key, tone: 'neutral' };
}

function typeRow(label: string | undefined, key: string): ToneLabel {
  return { label: label ?? key, tone: TYPE_TONE };
}

function toSection(
  title: string,
  buckets: readonly CategoryCountDto[],
  total: number,
  lookup: (key: string) => ToneLabel,
): BreakdownSection {
  return {
    title,
    rows: buckets.map((bucket) => {
      const meta = lookup(bucket.key);
      return {
        label: meta.label,
        count: bucket.count,
        percent: total > 0 ? Math.round((bucket.count / total) * 100) : 0,
        tone: meta.tone,
      };
    }),
  };
}

export function mapRequestReport(dto: RequestReportDto): ReportVm {
  return {
    title: 'Requests',
    total: dto.total,
    sections: [
      toSection('By status', dto.byStatus, dto.total, (key) => withTone(REQUEST_STATUS_META[key as RequestStatus], key)),
      toSection('By type', dto.byType, dto.total, (key) => typeRow(REQUEST_TYPE_LABEL[key as RequestType], key)),
      toSection('By priority', dto.byPriority, dto.total, (key) => withTone(REQUEST_PRIORITY_META[key as RequestPriority], key)),
    ],
  };
}

export function mapAssetReport(dto: AssetReportDto): ReportVm {
  return {
    title: 'Assets',
    total: dto.total,
    sections: [
      toSection('By status', dto.byStatus, dto.total, (key) => withTone(ASSET_STATUS_META[key as AssetStatus], key)),
      toSection('By type', dto.byType, dto.total, (key) => typeRow(ASSET_TYPE_LABEL[key as AssetType], key)),
    ],
  };
}
