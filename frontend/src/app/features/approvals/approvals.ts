import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';

import { RequestsService } from '../requests/requests.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApprovalStepVm, REQUEST_TYPE_ICON, RequestVm } from '../requests/requests.models';
import { StatusChip } from '../../shared/status-chip/status-chip';

interface PendingApproval {
  readonly request: RequestVm;
  readonly step: ApprovalStepVm;
  readonly position: number;
  readonly total: number;
}

/** "Pending your approval" queue — submitted requests whose current step's role the user holds,
 *  derived client-side from GET /requests (there's no per-user endpoint). The actual approve/
 *  reject happens on the request detail; this screen routes there. The API re-checks role +
 *  department scope, so this filter is a UX convenience, not an authorization decision.
 *  SLA / risk columns and the Approved/Rejected/Escalated tabs from the design are omitted —
 *  the domain has no SLA, risk, or per-user decision history to back them (see ui-upgrade-plan). */
@Component({
  selector: 'app-approvals',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule, MatCardModule, StatusChip],
  templateUrl: './approvals.html',
  styleUrl: './approvals.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Approvals {
  private readonly service = inject(RequestsService);
  private readonly auth = inject(AuthService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly typeIcon = REQUEST_TYPE_ICON;

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
