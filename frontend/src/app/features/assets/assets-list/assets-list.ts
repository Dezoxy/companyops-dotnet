import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AssetsService } from '../assets.service';
import { ASSET_STATUS_META, ASSET_TYPE_LABEL, AssetStatus, AssetType } from '../assets.models';
import { AuthService } from '../../../core/auth/auth.service';
import { RegisterAssetDialog } from '../register-asset-dialog';
import { StatusChip } from '../../../shared/status-chip/status-chip';

/** Asset inventory: a filterable, paged table over GET /assets. IT Admin can register; the
 *  read-only Auditor sees the list but not the register action (the API enforces both). */
@Component({
  selector: 'app-assets-list',
  imports: [
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
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

  protected readonly assets = this.service.assets;
  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly canManage = computed(() => this.auth.hasRole('ItAdmin'));

  protected readonly columns = ['tag', 'name', 'type', 'status', 'assignee', 'actions'];
  protected readonly statusOptions = Object.entries(ASSET_STATUS_META).map(([value, meta]) => ({
    value: value as AssetStatus,
    label: meta.label,
  }));
  protected readonly typeOptions = Object.entries(ASSET_TYPE_LABEL).map(([value, label]) => ({
    value: value as AssetType,
    label,
  }));

  protected readonly search = signal('');
  protected readonly statusFilter = signal<AssetStatus | 'all'>('all');
  protected readonly typeFilter = signal<AssetType | 'all'>('all');
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.statusFilter();
    const type = this.typeFilter();
    return this.assets().filter(
      (a) =>
        (status === 'all' || a.status === status) &&
        (type === 'all' || a.type === type) &&
        (term === '' || a.name.toLowerCase().includes(term) || a.tag.toLowerCase().includes(term)),
    );
  });

  protected readonly paged = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  constructor() {
    this.service.loadAll();
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    this.pageIndex.set(0);
  }

  protected onStatus(value: AssetStatus | 'all'): void {
    this.statusFilter.set(value);
    this.pageIndex.set(0);
  }

  protected onType(value: AssetType | 'all'): void {
    this.typeFilter.set(value);
    this.pageIndex.set(0);
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  protected refresh(): void {
    this.service.loadAll();
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
            error: () => this.snack.open("Couldn't register the asset (the tag may be taken).", 'Dismiss', { duration: 6000 }),
          });
      });
  }
}
