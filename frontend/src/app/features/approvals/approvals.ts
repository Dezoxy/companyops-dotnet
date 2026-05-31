import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';

import { RequestsService } from '../requests/requests.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApprovalStepVm, RequestVm } from '../requests/requests.models';

interface PendingApproval {
  readonly request: RequestVm;
  readonly step: ApprovalStepVm;
  readonly position: number;
  readonly total: number;
}

/** "Pending your approval" queue — submitted requests whose current step's role the user holds,
 *  derived client-side from GET /requests (there's no per-user endpoint). The actual approve/
 *  reject happens on the request detail; this screen routes there. The API re-checks role +
 *  department scope, so this filter is a UX convenience, not an authorization decision. */
@Component({
  selector: 'app-approvals',
  imports: [DatePipe, RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatProgressBarModule, MatCardModule],
  templateUrl: './approvals.html',
  styleUrl: './approvals.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Approvals {
  private readonly service = inject(RequestsService);
  private readonly auth = inject(AuthService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly columns = ['title', 'type', 'step', 'created', 'actions'];

  protected readonly queue = computed<PendingApproval[]>(() => {
    const roles = this.auth.roles();
    return this.service
      .requests()
      .filter((request) => request.status === 'Submitted')
      .map((request) => {
        const step = request.approvalSteps.find((s) => s.isCurrent) ?? null;
        return step ? { request, step, position: step.order, total: request.approvalSteps.length } : null;
      })
      .filter((entry): entry is PendingApproval => entry !== null && roles.includes(entry.step.requiredRole));
  });

  constructor() {
    this.service.loadAll();
  }

  protected refresh(): void {
    this.service.loadAll();
  }
}
