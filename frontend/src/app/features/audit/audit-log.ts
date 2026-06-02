import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap, tap } from 'rxjs';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuditService } from './audit.service';
import { AuditLogVm } from './audit.models';
import { StatusChip } from '../../shared/status-chip/status-chip';

/** Read-only audit trail (GET /audit-logs, Auditor/IT-Admin gated). Server-paged — the trail can be
 *  large. Action/actor/date filtering needs backend filter params (not yet supported) — deferred;
 *  see docs/ui-upgrade-plan.md. The API has no write path; the log is append-only by design. */
@Component({
  selector: 'app-audit-log',
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusChip,
  ],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditLog {
  private readonly service = inject(AuditService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly logs = signal<readonly AuditLogVm[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  protected readonly rangeStart = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  protected readonly pageNumbers = computed<(number | null)[]>(() => {
    const last = this.totalPages();
    const current = this.page();
    const wanted = new Set([1, last, current, current - 1, current + 1]);
    const pages = [...wanted].filter((p) => p >= 1 && p <= last).sort((a, b) => a - b);
    const out: (number | null)[] = [];
    let prev = 0;
    for (const p of pages) {
      if (p - prev > 1) {
        out.push(null);
      }
      out.push(p);
      prev = p;
    }
    return out;
  });

  private readonly pageRequest = new Subject<number>();

  constructor() {
    this.pageRequest
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(false);
        }),
        switchMap((page) =>
          this.service.fetchPageResult(page, this.pageSize()).pipe(
            catchError(() => {
              this.error.set(true);
              this.loading.set(false);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.logs.set(res.items);
        this.total.set(res.total);
        this.page.set(res.page);
        this.pageSize.set(res.pageSize);
        this.loading.set(false);
      });

    this.pageRequest.next(1);
  }

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.page() && !this.loading()) {
      this.pageRequest.next(page);
    }
  }

  protected refresh(): void {
    this.pageRequest.next(this.page());
  }
}
