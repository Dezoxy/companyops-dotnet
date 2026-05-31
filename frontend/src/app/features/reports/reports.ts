import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { ReportsService } from './reports.service';
import { ReportVm } from './reports.models';

/**
 * Reports & Analytics: aggregate breakdowns of requests and assets, computed server-side
 * (`GET /reports/*`, GROUP BY) and rendered as proportional bars. Read-only; the route gates it to
 * the oversight roles. No charting dependency — the bars are themed CSS over the shared tone tokens.
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

  /** Both reports, once loaded, in render order. */
  protected readonly reports = computed<readonly ReportVm[]>(() =>
    [this.service.requestReport(), this.service.assetReport()].filter((report): report is ReportVm => report !== null),
  );

  constructor() {
    this.service.load();
  }

  protected refresh(): void {
    this.service.load();
  }
}
