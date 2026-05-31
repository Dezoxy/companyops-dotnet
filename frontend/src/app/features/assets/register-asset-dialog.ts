import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { ASSET_TYPE_LABEL, AssetType, RegisterAssetInput } from './assets.models';

/** Dialog to register a new asset. Returns a RegisterAssetInput (or undefined on cancel). */
@Component({
  selector: 'app-register-asset-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Register asset</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Tag</mat-label>
          <input matInput formControlName="tag" maxlength="50" placeholder="e.g. AST-2026-0001" />
          @if (form.controls.tag.hasError('required') && form.controls.tag.touched) {
            <mat-error>Tag is required.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" maxlength="200" placeholder="e.g. MacBook Pro 16" />
          @if (form.controls.name.hasError('required') && form.controls.name.touched) {
            <mat-error>Name is required.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Type</mat-label>
          <mat-select formControlName="type">
            @for (option of types; track option.value) {
              <mat-option [value]="option.value">{{ option.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button [disabled]="form.invalid" (click)="confirm()">Register</button>
    </mat-dialog-actions>
  `,
  styles: `
    .form {
      display: flex;
      flex-direction: column;
      min-width: 360px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
})
export class RegisterAssetDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject<MatDialogRef<RegisterAssetDialog, RegisterAssetInput>>(MatDialogRef);

  protected readonly types = Object.entries(ASSET_TYPE_LABEL).map(([value, label]) => ({
    value: value as AssetType,
    label,
  }));

  protected readonly form = this.fb.nonNullable.group({
    tag: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    type: this.fb.nonNullable.control<AssetType>('Laptop', Validators.required),
  });

  protected confirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { tag, name, type } = this.form.getRawValue();
    this.ref.close({ tag: tag.trim(), name: name.trim(), type });
  }
}
