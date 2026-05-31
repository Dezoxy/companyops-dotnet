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

import { RequestsService } from '../requests.service';
import { ApproverRole, RequestVm } from '../requests.models';
import { StatusChip } from '../../../shared/status-chip/status-chip';
import { AuthService } from '../../../core/auth/auth.service';
import { DecisionDialog, DecisionDialogData, DecisionDialogResult } from '../decision-dialog/decision-dialog';

type LoadState = 'loading' | 'loaded' | 'notfound' | 'error';

/** Request detail: the full request, its approval-chain timeline, and the actions the current
 *  user may take (submit own draft; approve/reject the step their role owns). Buttons are
 *  shown/hidden for UX only — the API re-validates submit-own, department scope, and stage. */
@Component({
  selector: 'app-request-detail',
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
  templateUrl: './request-detail.html',
  styleUrl: './request-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(RequestsService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly request = signal<RequestVm | null>(null);
  protected readonly status = signal<LoadState>('loading');
  protected readonly acting = signal(false);

  /** The step the chain is waiting on, if any. */
  protected readonly currentStep = computed(() => this.request()?.approvalSteps.find((s) => s.isCurrent) ?? null);

  /** Submit is available to the owner of a Draft. */
  protected readonly canSubmit = computed(() => {
    const r = this.request();
    return !!r && r.status === 'Draft' && r.requesterId === this.auth.userId();
  });

  /** Approve/reject is available when the current step's role is one the user holds. */
  protected readonly canDecide = computed(() => {
    const r = this.request();
    const step = this.currentStep();
    return !!r && r.status === 'Submitted' && !!step && this.auth.hasRole(step.requiredRole);
  });

  /** Fulfil is available to IT Admin once the request is Approved (helpdesk + procurement). */
  protected readonly canFulfill = computed(() => {
    const r = this.request();
    return !!r && r.status === 'Approved' && this.auth.hasRole('ItAdmin' satisfies ApproverRole);
  });

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
        next: (request) => {
          this.request.set(request);
          this.status.set('loaded');
        },
        error: (err: HttpErrorResponse) => this.status.set(err.status === 404 ? 'notfound' : 'error'),
      });
  }

  protected submit(): void {
    const r = this.request();
    if (!r || this.acting()) {
      return;
    }
    this.run(this.service.submit(r.id), 'Request submitted for approval.');
  }

  protected fulfill(): void {
    const r = this.request();
    if (!r || this.acting()) {
      return;
    }
    this.run(this.service.fulfill(r.id), 'Request fulfilled.');
  }

  protected decide(action: 'approve' | 'reject'): void {
    const r = this.request();
    if (!r || this.acting()) {
      return;
    }
    this.dialog
      .open<DecisionDialog, DecisionDialogData, DecisionDialogResult>(DecisionDialog, {
        data: { action, requestTitle: r.title },
        width: '480px',
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) {
          return;
        }
        const op$ =
          action === 'approve' ? this.service.approve(r.id, result.text || undefined) : this.service.reject(r.id, result.text);
        this.run(op$, action === 'approve' ? 'Request approved.' : 'Request rejected.');
      });
  }

  /** Shared action runner: reflect the updated request, toast success, or toast a failure. */
  private run(op$: Observable<RequestVm>, success: string): void {
    this.acting.set(true);
    op$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.request.set(updated);
        this.acting.set(false);
        this.snack.open(success, 'Dismiss', { duration: 4000 });
      },
      error: () => {
        this.acting.set(false);
        this.snack.open("Action couldn't be completed — you may not be allowed, or the request has moved on.", 'Dismiss', {
          duration: 6000,
        });
      },
    });
  }
}
