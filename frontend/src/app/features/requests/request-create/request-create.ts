import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { RequestsService } from '../requests.service';
import { REQUEST_TYPE_LABEL, RequestType } from '../requests.models';

/** Create a request. Only the API-backed fields (title/type/description) — department comes from
 *  the JWT, and Priority/Cost Center/Approval Path from the mockup aren't in the domain. Two
 *  actions mirror the two-step API: save as Draft, or create then submit for approval. */
@Component({
  selector: 'app-request-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './request-create.html',
  styleUrl: './request-create.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestCreate {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(RequestsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly types = Object.entries(REQUEST_TYPE_LABEL).map(([value, label]) => ({
    value: value as RequestType,
    label,
  }));

  protected readonly submitting = signal(false);
  protected readonly error = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    type: this.fb.nonNullable.control<RequestType | ''>('', Validators.required),
    description: [''],
  });

  /** Create the request; when `thenSubmit`, chain the submit so it goes straight to approval. */
  protected save(thenSubmit: boolean): void {
    if (this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(false);

    const { title, type, description } = this.form.getRawValue();
    const created$ = this.service.create({ title, type: type as RequestType, description: description || null });
    const flow$ = thenSubmit ? created$.pipe(switchMap((request) => this.service.submit(request.id))) : created$;

    flow$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (request) => {
        this.submitting.set(false);
        this.router.navigate(['/requests', request.id]);
      },
      error: () => {
        this.error.set(true);
        this.submitting.set(false);
      },
    });
  }
}
