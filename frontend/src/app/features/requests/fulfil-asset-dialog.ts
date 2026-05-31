import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AssetsService } from '../assets/assets.service';

/**
 * IT picks the in-stock asset to assign when fulfilling an asset-lifecycle request. Reuses the
 * assets feature's API client (IT Admin holds ReadAssets) and offers only InStock assets — the
 * only ones {@link AssetsService.assign} / fulfillment can transition. Returns the chosen asset
 * id, or undefined on cancel. The API re-validates that the asset is still assignable.
 */
@Component({
  selector: 'app-fulfil-asset-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressBarModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Assign an asset</h2>
    <mat-dialog-content>
      <p class="muted">Pick an in-stock asset to assign to the requester. This completes the request.</p>
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Loading assets" />
      } @else if (loadError()) {
        <p class="muted">Couldn't load assets — check your connection and try again.</p>
      } @else if (inStock().length === 0) {
        <p class="muted">No in-stock assets are available — register one in the Assets console first.</p>
      } @else {
        <mat-form-field appearance="outline" class="full">
          <mat-label>In-stock asset</mat-label>
          <mat-select [formControl]="assetId">
            @for (asset of inStock(); track asset.id) {
              <mat-option [value]="asset.id">{{ asset.tag }} — {{ asset.name }} ({{ asset.typeLabel }})</mat-option>
            }
          </mat-select>
          @if (assetId.hasError('required') && assetId.touched) {
            <mat-error>Select an asset to assign.</mat-error>
          }
        </mat-form-field>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button [disabled]="assetId.invalid || inStock().length === 0" (click)="confirm()">
        Assign &amp; fulfil
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .full {
      width: 100%;
      min-width: 360px;
    }
    .muted {
      color: var(--mat-sys-on-surface-variant);
      margin-top: 0;
    }
  `,
})
export class FulfilAssetDialog {
  private readonly assets = inject(AssetsService);
  private readonly ref = inject<MatDialogRef<FulfilAssetDialog, string>>(MatDialogRef);

  protected readonly loading = this.assets.loading;
  protected readonly loadError = this.assets.error;
  protected readonly inStock = computed(() => this.assets.assets().filter((asset) => asset.status === 'InStock'));
  protected readonly assetId = new FormControl('', { nonNullable: true, validators: [Validators.required] });

  constructor() {
    this.assets.loadAll();
  }

  protected confirm(): void {
    if (this.assetId.invalid) {
      this.assetId.markAsTouched();
      return;
    }
    this.ref.close(this.assetId.value);
  }
}
