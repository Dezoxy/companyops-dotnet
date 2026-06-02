import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';

import { AuthService } from '../../../core/auth/auth.service';
import { RequestsService } from '../requests.service';
import { REQUEST_TYPE_ICON, RequestVm } from '../requests.models';
import { StatusChip } from '../../../shared/status-chip/status-chip';

/** Requests list: a server-paged table over GET /requests (the API scopes rows by role). Owns its
 *  own page fetch (fetchPageResult) rather than the shared list signal, so paging here never
 *  clobbers what the Approvals/Fulfilment queue screens read. Pagination is server-side so the
 *  footer shows the true total. Status/type filtering needs backend filter params (not yet
 *  supported) — deferred; see docs/ui-upgrade-plan.md. */
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
  private readonly destroyRef = inject(DestroyRef);

  protected readonly requests = signal<readonly RequestVm[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

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
    this.load(1);
  }

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.page() && !this.loading()) {
      this.load(page);
    }
  }

  protected refresh(): void {
    this.load(this.page());
  }

  private load(page: number): void {
    this.loading.set(true);
    this.error.set(false);
    this.service
      .fetchPageResult(page, this.pageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.requests.set(res.items);
          this.total.set(res.total);
          this.page.set(res.page);
          this.pageSize.set(res.pageSize);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }
}
