import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { RequestsService } from '../requests.service';
import { RequestVm } from '../requests.models';
import { StatusChip } from '../../../shared/status-chip/status-chip';

type LoadState = 'loading' | 'loaded' | 'notfound' | 'error';

/** Request detail: the full request plus its approval-chain timeline (GET /requests/{id}).
 *  Read-only in Phase 14a — submit/approve/reject actions land in 14b. */
@Component({
  selector: 'app-request-detail',
  imports: [DatePipe, RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatProgressBarModule, StatusChip],
  templateUrl: './request-detail.html',
  styleUrl: './request-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(RequestsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly request = signal<RequestVm | null>(null);
  protected readonly status = signal<LoadState>('loading');

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
}
