import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AssetsService } from '../assets.service';
import { AssetHistoryVm, AssetVm } from '../assets.models';
import { AuthService } from '../../../core/auth/auth.service';
import { AssignAssetDialog } from '../assign-asset-dialog';
import { StatusChip } from '../../../shared/status-chip/status-chip';

type LoadState = 'loading' | 'loaded' | 'notfound' | 'error';

/** Asset detail: status + assignee, the lifecycle actions (IT-Admin only; gated by the current
 *  status), and the activity history. The aggregate enforces valid transitions; the API enforces
 *  the role — the buttons are UX. */
@Component({
  selector: 'app-asset-detail',
  imports: [
    DatePipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatDialogModule,
    MatSnackBarModule,
    StatusChip,
  ],
  templateUrl: './asset-detail.html',
  styleUrl: './asset-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(AssetsService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly asset = signal<AssetVm | null>(null);
  protected readonly history = signal<readonly AssetHistoryVm[]>([]);
  protected readonly historyError = signal(false);
  protected readonly status = signal<LoadState>('loading');
  protected readonly acting = signal(false);

  private readonly canManage = computed(() => this.auth.hasRole('ItAdmin'));
  private readonly current = computed(() => this.asset()?.status ?? null);

  protected readonly canAssign = computed(() => this.canManage() && this.current() === 'InStock');
  protected readonly canReclaim = computed(() => this.canManage() && this.current() === 'Assigned');
  protected readonly canRepair = computed(() => this.canManage() && (this.current() === 'InStock' || this.current() === 'Assigned'));
  protected readonly canReturn = computed(() => this.canManage() && this.current() === 'InRepair');
  // Retire is valid from any active state (including InRepair — an unrepairable asset) — this
  // matches Asset.Retire in the domain; the API re-checks regardless.
  protected readonly canRetire = computed(() => this.canManage() && this.current() !== null && this.current() !== 'Retired');
  protected readonly hasActions = computed(
    () => this.canAssign() || this.canReclaim() || this.canRepair() || this.canReturn() || this.canRetire(),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    if (!this.id) {
      this.status.set('notfound');
      return;
    }
    this.status.set('loading');
    this.service
      .getById(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (asset) => {
          this.asset.set(asset);
          this.status.set('loaded');
          this.loadHistory();
        },
        error: (err: HttpErrorResponse) => this.status.set(err.status === 404 ? 'notfound' : 'error'),
      });
  }

  protected assign(): void {
    const asset = this.asset();
    if (!asset || this.acting()) {
      return;
    }
    this.dialog
      .open(AssignAssetDialog, { width: '440px' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((userId: string | undefined) => {
        if (!userId) {
          return;
        }
        this.run(this.service.assign(asset.id, userId), 'Asset assigned.');
      });
  }

  protected reclaim(): void {
    this.act((id) => this.service.reclaim(id), 'Asset reclaimed.');
  }

  protected sendToRepair(): void {
    this.act((id) => this.service.sendToRepair(id), 'Asset sent for repair.');
  }

  protected returnFromRepair(): void {
    this.act((id) => this.service.returnFromRepair(id), 'Asset returned to stock.');
  }

  protected retire(): void {
    this.act((id) => this.service.retire(id), 'Asset retired.');
  }

  private act(operation: (id: string) => Observable<AssetVm>, success: string): void {
    const asset = this.asset();
    if (!asset || this.acting()) {
      return;
    }
    this.run(operation(asset.id), success);
  }

  private run(op$: Observable<AssetVm>, success: string): void {
    this.acting.set(true);
    op$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (asset) => {
        this.asset.set(asset);
        this.acting.set(false);
        this.snack.open(success, 'Dismiss', { duration: 4000 });
        this.loadHistory();
      },
      error: () => {
        this.acting.set(false);
        this.snack.open("Action couldn't be completed — the asset may have moved on.", 'Dismiss', { duration: 6000 });
      },
    });
  }

  private loadHistory(): void {
    this.historyError.set(false);
    this.service
      .history(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (entries) => this.history.set(entries),
        error: () => this.historyError.set(true),
      });
  }
}
