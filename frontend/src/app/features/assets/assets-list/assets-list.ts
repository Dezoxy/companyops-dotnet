import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap, takeUntil, tap } from 'rxjs';
import { DatePipe } from '@angular/common';
import { A11yModule } from '@angular/cdk/a11y';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AssetsService } from '../assets.service';
import { ASSET_TYPE_ICON, AssetHistoryVm, AssetVm } from '../assets.models';
import { AuthService } from '../../../core/auth/auth.service';
import { RegisterAssetDialog } from '../register-asset-dialog';
import { StatusChip } from '../../../shared/status-chip/status-chip';

/** Asset inventory: a server-paged table over GET /assets with a slide-over quick-view (summary +
 *  recent history). Lifecycle actions live on the full detail route (linked from the panel). IT
 *  Admin can register; the read-only Auditor sees the list but not the register action — the API
 *  enforces both, the UI only hides. */
@Component({
  selector: 'app-assets-list',
  imports: [
    DatePipe,
    A11yModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    StatusChip,
  ],
  templateUrl: './assets-list.html',
  styleUrl: './assets-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetsList {
  private readonly service = inject(AssetsService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly assets = signal<readonly AssetVm[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  protected readonly typeIcon = ASSET_TYPE_ICON;
  protected readonly canManage = computed(() => this.auth.hasRole('ItAdmin'));

  // Slide-over quick-view state. `historyCancel` aborts an in-flight history fetch when the panel
  // is closed or a different asset is opened, so opens don't accumulate live subscriptions.
  protected readonly selected = signal<AssetVm | null>(null);
  protected readonly history = signal<readonly AssetHistoryVm[]>([]);
  protected readonly historyLoading = signal(false);
  protected readonly historyError = signal(false);
  private readonly historyCancel = new Subject<void>();

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

  // Page stream + switchMap so a newer page request cancels an in-flight one.
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
        this.assets.set(res.items);
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

  /** Open the slide-over for a row and load its recent history. */
  protected openDetail(asset: AssetVm): void {
    this.historyCancel.next(); // abort any previous in-flight fetch
    this.selected.set(asset);
    this.history.set([]);
    this.historyError.set(false);
    this.historyLoading.set(true);
    this.service
      .history(asset.id)
      .pipe(takeUntil(this.historyCancel), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (entries) => {
          this.history.set(entries);
          this.historyLoading.set(false);
        },
        error: () => {
          this.historyError.set(true);
          this.historyLoading.set(false);
        },
      });
  }

  protected closeDetail(): void {
    this.historyCancel.next();
    this.selected.set(null);
  }

  protected register(): void {
    this.dialog
      .open(RegisterAssetDialog, { width: '480px' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((input) => {
        if (!input) {
          return;
        }
        this.service
          .register(input)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (asset) => this.router.navigate(['/assets', asset.id]),
            error: () =>
              this.snack.open("Couldn't register the asset (the tag may be taken).", 'Dismiss', { duration: 6000 }),
          });
      });
  }
}
