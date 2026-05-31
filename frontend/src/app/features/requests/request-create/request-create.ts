import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
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
import {
  REQUEST_CATEGORY_LABEL,
  REQUEST_PRIORITY_META,
  REQUEST_TYPE_LABEL,
  RequestCategory,
  RequestPriority,
  RequestType,
} from '../requests.models';

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
  protected readonly priorities = Object.entries(REQUEST_PRIORITY_META).map(([value, meta]) => ({
    value: value as RequestPriority,
    label: meta.label,
  }));
  protected readonly categories = Object.entries(REQUEST_CATEGORY_LABEL).map(([value, label]) => ({
    value: value as RequestCategory,
    label,
  }));

  protected readonly submitting = signal(false);
  protected readonly error = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    type: this.fb.nonNullable.control<RequestType | ''>('', Validators.required),
    priority: this.fb.nonNullable.control<RequestPriority>('Medium', Validators.required),
    category: this.fb.nonNullable.control<RequestCategory | ''>(''),
    description: [''],
  });

  // Reactive type → drives the conditional Category field (signals over manual change detection).
  private readonly typeValue = toSignal(this.form.controls.type.valueChanges, {
    initialValue: this.form.controls.type.value,
  });
  protected readonly isHelpdesk = computed(() => this.typeValue() === 'Helpdesk');

  constructor() {
    // Clear a stale category if the user switches away from Helpdesk (save() also guards).
    this.form.controls.type.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((type) => {
      if (type !== 'Helpdesk') {
        this.form.controls.category.setValue('');
      }
    });
  }

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

    const { title, type, priority, category, description } = this.form.getRawValue();
    const requestType = type as RequestType;
    const created$ = this.service.create({
      title,
      type: requestType,
      description: description || null,
      priority,
      // Category is helpdesk-only; never send one for other types.
      category: requestType === 'Helpdesk' ? category || null : null,
    });
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
