import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';

import { AuthService } from '../../../core/auth/auth.service';
import { RequestsService } from '../requests.service';
import { REQUEST_TYPE_ICON } from '../requests.models';
import { StatusChip } from '../../../shared/status-chip/status-chip';

/** Requests list: a server-paged table over GET /requests (the API scopes rows by role). Pagination
 *  is server-side so the footer shows the true total, not a capped page. Status/type filtering needs
 *  backend filter params (not yet supported) — deferred; see docs/ui-upgrade-plan.md. */
@Component({
  selector: 'app-requests-list',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule, MatCardModule, StatusChip],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsList {
  private readonly service = inject(RequestsService);
  private readonly auth = inject(AuthService);

  protected readonly requests = this.service.requests;
  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly total = this.service.total;
  protected readonly page = this.service.page;
  protected readonly pageSize = this.service.pageSize;
  protected readonly totalPages = this.service.totalPages;

  protected readonly typeIcon = REQUEST_TYPE_ICON;
  // Only Employees create requests (docs/security.md); the API still enforces.
  protected readonly canCreate = computed(() => this.auth.hasRole('Employee'));

  // "Showing 1–50 of 142" — the 1-based range of the current page.
  protected readonly rangeStart = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  // Windowed page numbers: first, last, and the current ±1, with `null` marking an ellipsis gap.
  protected readonly pageNumbers = computed<(number | null)[]>(() => {
    const last = this.totalPages();
    const current = this.page();
    const wanted = new Set([1, last, current, current - 1, current + 1]);
    const pages = [...wanted].filter((p) => p >= 1 && p <= last).sort((a, b) => a - b);
    const out: (number | null)[] = [];
    let prev = 0;
    for (const p of pages) {
      if (p - prev > 1) {
        out.push(null); // gap
      }
      out.push(p);
      prev = p;
    }
    return out;
  });

  constructor() {
    this.service.loadAll();
  }

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.page() && !this.loading()) {
      this.service.loadAll(page, this.pageSize());
    }
  }

  protected refresh(): void {
    this.service.loadAll(this.page(), this.pageSize());
  }
}
