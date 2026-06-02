import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AuthService } from '../../core/auth/auth.service';
import { RequestsService } from '../requests/requests.service';
import { ReportsService } from '../reports/reports.service';
import { IntegrationsService } from '../integrations/integrations.service';
import { KpiCounts } from '../reports/reports.models';
import { REQUEST_TYPE_ICON, RequestStatus, RequestVm } from '../requests/requests.models';
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
const isTerminal = (s: RequestStatus): boolean => TERMINAL_STATUSES.includes(s);

// Roles the API grants the read endpoints (docs/security.md; AuthSetup ReadReports/ReadIntegrations).
// The UI mirrors these so it never fires a call that would 403, and hides panels a role can't read.
const REPORTS_ROLES = ['Manager', 'Finance', 'ItAdmin', 'Auditor'] as const;
const INTEGRATIONS_ROLES = ['ItAdmin', 'Auditor'] as const;

/**
 * Dashboard Overview: KPI cards + recent activity + system status.
 *
 * Role-adaptive, mirroring the API's read policies (the API still enforces; this only avoids
 * guaranteed-403 calls and hides panels a role can't use — the sanctioned "UI may hide" pattern):
 *   - `/reports` (Manager/Finance/ItAdmin/Auditor) → org-wide KPI aggregates. Employees instead get
 *     personal KPIs computed from their own `/requests` list, so their dashboard keeps working.
 *   - `/integrations` (ItAdmin/Auditor) → system-status panel; hidden for everyone else.
 *   - `/requests` (any authenticated, server-scoped) → recent activity for all.
 * The SPA stays a thin client: it only summarizes what the API returns, no business logic.
 */
@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressBarModule, StatusChip],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly auth = inject(AuthService);
  private readonly requestsService = inject(RequestsService);
  private readonly reports = inject(ReportsService);
  private readonly integrations = inject(IntegrationsService);

  protected readonly canViewReports = computed(() => REPORTS_ROLES.some((r) => this.auth.hasRole(r)));
  protected readonly canViewIntegrations = computed(() =>
    INTEGRATIONS_ROLES.some((r) => this.auth.hasRole(r)),
  );

  // Top progress bar: any section the role actually loads. Each panel renders its own error/empty.
  protected readonly loading = computed(
    () =>
      this.requestsService.loading() ||
      (this.canViewReports() && this.reports.loading()) ||
      (this.canViewIntegrations() && this.integrations.loading()),
  );
  // KPI source is /reports for privileged roles, otherwise the /requests list — so its error follows.
  protected readonly kpiError = computed(() =>
    this.canViewReports() ? this.reports.error() : this.requestsService.error(),
  );
  protected readonly recentError = this.requestsService.error;
  protected readonly systemError = this.integrations.error;

  protected readonly stats = computed<readonly StatCard[]>(() =>
    this.canViewReports() ? this.orgStats() : this.personalStats(),
  );

  // Org-wide KPIs from the /reports GROUP BY aggregates (true totals, not capped by a list page).
  private orgStats(): readonly StatCard[] {
    const counts: KpiCounts | null = this.reports.kpiCounts();
    const assetsTotal = this.reports.assetReport()?.total ?? 0;
    const byStatus = counts?.byStatus ?? {};
    const byPriority = counts?.byPriority ?? {};
    const total = counts?.total ?? 0;
    const terminal = TERMINAL_STATUSES.reduce((sum, s) => sum + (byStatus[s] ?? 0), 0);
    const pending = byStatus['Submitted'] ?? 0;
    const critical = byPriority['Critical'] ?? 0;

    return [
      { label: 'Active requests', value: Math.max(0, total - terminal), icon: 'description' },
      {
        label: 'Pending approvals',
        value: pending,
        icon: 'pending_actions',
        badge: pending > 0 ? { text: 'Action required', tone: 'progress' } : undefined,
      },
      { label: 'Critical priority', value: critical, icon: 'priority_high', emphasis: critical > 0 },
      { label: 'Managed assets', value: assetsTotal, icon: 'inventory_2' },
    ];
  }

  // Personal KPIs for roles without /reports access (Employee): counted from their own request list.
  private personalStats(): readonly StatCard[] {
    const mine = this.requestsService.requests();
    const count = (predicate: (r: RequestVm) => boolean) => mine.filter(predicate).length;
    const pending = count((r) => r.status === 'Submitted');
    const critical = count((r) => r.priority === 'Critical' && !isTerminal(r.status));

    return [
      { label: 'My active requests', value: count((r) => !isTerminal(r.status)), icon: 'description' },
      {
        label: 'Awaiting approval',
        value: pending,
        icon: 'pending_actions',
        badge: pending > 0 ? { text: 'In review', tone: 'progress' } : undefined,
      },
      { label: 'Critical priority', value: critical, icon: 'priority_high', emphasis: critical > 0 },
      { label: 'Completed', value: count((r) => r.status === 'Completed'), icon: 'check_circle' },
    ];
  }

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
    this.requestsService.loadAll();
    if (this.canViewReports()) {
      this.reports.load();
    }
    if (this.canViewIntegrations()) {
      this.integrations.load();
    }
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
