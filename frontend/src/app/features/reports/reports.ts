import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { ReportsService } from './reports.service';
import { ReportVm } from './reports.models';

interface ReportKpi {
  readonly label: string;
  readonly value: number;
  readonly icon: string;
}

/**
 * Reports & Analytics: aggregate breakdowns of requests and assets, computed server-side
 * (`GET /reports/*`, GROUP BY) and rendered as KPI cards + proportional bars. Read-only; the route
 * gates it to the oversight roles. No charting dependency — the bars are themed CSS over the shared
 * tone tokens. Spend / avg-approval-time / time-series / AI insights from the design are omitted —
 * the domain has no such data (see docs/ui-upgrade-plan).
 */
@Component({
  selector: 'app-reports',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reports {
  private readonly service = inject(ReportsService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;

  /** Headline KPIs from the real aggregates (no fabricated spend / approval-time metrics). */
  protected readonly kpis = computed<readonly ReportKpi[]>(() => {
    const counts = this.service.kpiCounts();
    const assetsTotal = this.service.assetReport()?.total ?? 0;
    return [
      { label: 'Total requests', value: counts?.total ?? 0, icon: 'description' },
      { label: 'Pending approvals', value: counts?.byStatus['Submitted'] ?? 0, icon: 'pending_actions' },
      { label: 'Critical priority', value: counts?.byPriority['Critical'] ?? 0, icon: 'priority_high' },
      { label: 'Managed assets', value: assetsTotal, icon: 'inventory_2' },
    ];
  });

  /** Both reports, once loaded, in render order. */
  protected readonly reports = computed<readonly ReportVm[]>(() =>
    [this.service.requestReport(), this.service.assetReport()].filter((report): report is ReportVm => report !== null),
  );

  // Show the KPI row only once the counts are loaded (avoids a flash of zeros during initial load).
  protected readonly hasData = computed(() => this.service.kpiCounts() !== null);

  constructor() {
    this.service.load();
  }

  protected refresh(): void {
    this.service.load();
  }
}
