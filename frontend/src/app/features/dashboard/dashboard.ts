import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { RequestsService } from '../requests/requests.service';
import { ReportsService } from '../reports/reports.service';
import { IntegrationsService } from '../integrations/integrations.service';
import { KpiCounts } from '../reports/reports.models';
import { REQUEST_TYPE_ICON, RequestStatus } from '../requests/requests.models';
import { StatusChip, Tone } from '../../shared/status-chip/status-chip';

/** A KPI tile: an icon, a label, a value, and an optional derived badge/emphasis. No fabricated
 *  trend deltas — the backend has no time-series, so we only surface states we can actually compute. */
interface StatCard {
  readonly label: string;
  readonly value: number;
  readonly icon: string;
  readonly badge?: { readonly text: string; readonly tone: Tone };
  readonly emphasis?: boolean;
}

/** A single row in the System Status panel, derived from the integration outbox snapshot. */
interface SystemRow {
  readonly name: string;
  readonly detail: string;
  readonly state: SystemState;
}
type SystemState = 'operational' | 'degraded' | 'down';

// Statuses that take a request out of the active pipeline — used to derive "Active requests".
const TERMINAL_STATUSES: readonly RequestStatus[] = ['Completed', 'Rejected', 'Cancelled'];

/**
 * Dashboard Overview: KPI cards + recent activity + system status.
 *
 * Composes three existing feature services — KPI totals come from the `/reports` GROUP BY
 * aggregates (accurate, not capped by a list page), recent activity from the `/requests` list,
 * and system status from the `/integrations` outbox snapshot. The SPA stays a thin client: it
 * only summarizes what the API returns, no business logic (AGENTS.md / frontend/CLAUDE.md).
 */
@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressBarModule, StatusChip],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly requestsService = inject(RequestsService);
  private readonly reports = inject(ReportsService);
  private readonly integrations = inject(IntegrationsService);

  // Any section still loading drives the top progress bar; each panel renders its own error/empty.
  protected readonly loading = computed(
    () => this.reports.loading() || this.requestsService.loading() || this.integrations.loading(),
  );
  protected readonly kpiError = this.reports.error;
  protected readonly recentError = this.requestsService.error;
  protected readonly systemError = this.integrations.error;

  protected readonly stats = computed<readonly StatCard[]>(() => {
    const counts: KpiCounts | null = this.reports.kpiCounts();
    const assetsTotal = this.reports.assetReport()?.total ?? 0;
    const byStatus = counts?.byStatus ?? {};
    const byPriority = counts?.byPriority ?? {};
    const total = counts?.total ?? 0;
    const terminal = TERMINAL_STATUSES.reduce((sum, s) => sum + (byStatus[s] ?? 0), 0);
    const active = Math.max(0, total - terminal);
    const pending = byStatus['Submitted'] ?? 0;
    const critical = byPriority['Critical'] ?? 0;

    return [
      { label: 'Active requests', value: active, icon: 'description' },
      {
        label: 'Pending approvals',
        value: pending,
        icon: 'pending_actions',
        badge: pending > 0 ? { text: 'Action required', tone: 'progress' } : undefined,
      },
      { label: 'Critical priority', value: critical, icon: 'priority_high', emphasis: critical > 0 },
      { label: 'Managed assets', value: assetsTotal, icon: 'inventory_2' },
    ];
  });

  // Five most recent requests for the activity table.
  protected readonly recent = computed(() =>
    [...this.requestsService.requests()]
      .sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime())
      .slice(0, 5)
      .map((r) => ({
        id: r.id,
        shortId: r.id.slice(0, 8).toUpperCase(),
        title: r.title,
        typeLabel: r.typeLabel,
        typeIcon: REQUEST_TYPE_ICON[r.type],
        statusMeta: r.statusMeta,
        ago: this.ago(r.createdAt),
      })),
  );

  // System status from the integration outbox snapshot. Tile labels are stable (set in the
  // integrations mapper); read by name and default to 0 so a shape change degrades, not throws.
  protected readonly systemRows = computed<readonly SystemRow[]>(() => {
    const status = this.integrations.status();
    if (!status) {
      return [];
    }
    const tile = (label: string) => status.tiles.find((t) => t.label === label)?.value ?? 0;
    const pending = tile('Pending');
    const published = tile('Published');
    const failed = tile('Failed');
    const processed = tile('Processed by worker');

    return [
      {
        name: 'Message outbox',
        detail: pending > 0 ? `${pending} pending` : 'Drained',
        state: failed > 0 || pending > 0 ? 'degraded' : 'operational',
      },
      {
        name: 'Event publishing',
        detail: failed > 0 ? `${failed} failed` : `${published} published`,
        state: failed > 0 ? 'down' : 'operational',
      },
      {
        name: 'Background worker',
        detail: `${processed} processed`,
        // The snapshot has no liveness signal; a failed event is the only thing that hints the
        // worker isn't keeping up, so mirror it rather than always reporting green.
        state: failed > 0 ? 'degraded' : 'operational',
      },
    ];
  });

  // Worst row state wins for the overall indicator.
  protected readonly systemOverall = computed<SystemState>(() => {
    const rows = this.systemRows();
    if (rows.some((r) => r.state === 'down')) {
      return 'down';
    }
    if (rows.some((r) => r.state === 'degraded')) {
      return 'degraded';
    }
    return 'operational';
  });

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.reports.load();
    this.requestsService.loadAll();
    this.integrations.load();
  }

  /** Compact relative time ("10m ago", "3h ago", "2d ago", or a short date past a week).
   *  Computed once when the list signal changes; on a refresh-driven overview the value only
   *  goes stale between refreshes, which the Refresh button (and route re-entry) resolve. */
  private ago(date: Date): string {
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
    if (seconds < 60) {
      return 'just now';
    }
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return `${minutes}m ago`;
    }
    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return `${hours}h ago`;
    }
    const days = Math.floor(hours / 24);
    if (days < 7) {
      return `${days}d ago`;
    }
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
