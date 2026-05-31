import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

export interface DecisionDialogData {
  readonly action: 'approve' | 'reject';
  readonly requestTitle: string;
}

export interface DecisionDialogResult {
  /** Approve note (optional) or reject reason (required). */
  readonly text: string;
}

/** Small confirm dialog for an approve/reject decision. Reject requires a reason; approve takes
 *  an optional note. The actual API call + authorization live in the caller / the API. */
@Component({
  selector: 'app-decision-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isReject ? 'Reject request' : 'Approve request' }}</h2>
    <mat-dialog-content>
      <p class="subtitle">{{ data.requestTitle }}</p>
      <mat-form-field appearance="outline" class="full">
        <mat-label>{{ isReject ? 'Reason' : 'Note (optional)' }}</mat-label>
        <textarea
          matInput
          [formControl]="text"
          rows="4"
          [placeholder]="isReject ? 'Why is this being rejected?' : 'Optional note for the audit trail.'"
        ></textarea>
        @if (text.hasError('required') && text.touched) {
          <mat-error>A reason is required to reject.</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button (click)="confirm()">{{ isReject ? 'Reject' : 'Approve' }}</button>
    </mat-dialog-actions>
  `,
  styles: `
    .subtitle {
      color: var(--mat-sys-on-surface-variant);
      margin: 0 0 12px;
    }
    .full {
      width: 100%;
      min-width: 360px;
    }
  `,
})
export class DecisionDialog {
  protected readonly data = inject<DecisionDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<DecisionDialog, DecisionDialogResult>>(MatDialogRef);

  protected readonly isReject = this.data.action === 'reject';
  protected readonly text = new FormControl('', {
    nonNullable: true,
    validators: this.isReject ? [Validators.required, Validators.maxLength(1000)] : [Validators.maxLength(1000)],
  });

  protected confirm(): void {
    if (this.text.invalid) {
      this.text.markAsTouched();
      return;
    }
    this.ref.close({ text: this.text.value.trim() });
  }
}
