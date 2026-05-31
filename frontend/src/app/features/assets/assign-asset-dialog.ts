import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

/** Dialog to assign an asset to a user. Returns the user id (or undefined on cancel). No user
 *  directory yet, so the IT Admin enters the user's id (sub) — the API stores it as-is. */
@Component({
  selector: 'app-assign-asset-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Assign asset</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>User id</mat-label>
        <input matInput [formControl]="userId" maxlength="64" placeholder="Keycloak user id (sub)" />
        @if (userId.hasError('required') && userId.touched) {
          <mat-error>A user id is required.</mat-error>
        }
        <mat-hint>No directory yet — paste the user's id (sub).</mat-hint>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button [disabled]="userId.invalid" (click)="confirm()">Assign</button>
    </mat-dialog-actions>
  `,
  styles: `
    .full {
      width: 100%;
      min-width: 360px;
    }
  `,
})
export class AssignAssetDialog {
  private readonly ref = inject<MatDialogRef<AssignAssetDialog, string>>(MatDialogRef);

  protected readonly userId = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(64)],
  });

  protected confirm(): void {
    if (this.userId.invalid) {
      this.userId.markAsTouched();
      return;
    }
    this.ref.close(this.userId.value.trim());
  }
}
