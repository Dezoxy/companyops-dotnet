import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { RequestsService } from '../requests/requests.service';
import { RequestStatus } from '../requests/requests.models';
import { StatusChip } from '../../shared/status-chip/status-chip';

interface StatCard {
  readonly label: string;
  readonly value: number;
  readonly icon: string;
}

/** Landing dashboard: status counts + recent requests, summarized client-side from the shared
 *  request list. Server-side aggregations / reporting are a later phase (18) — not invented here. */
@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressBarModule, StatusChip],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly service = inject(RequestsService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  private readonly requests = this.service.requests;

  protected readonly stats = computed<readonly StatCard[]>(() => {
    const count = (status: RequestStatus) => this.requests().filter((r) => r.status === status).length;
    return [
      { label: 'Awaiting approval', value: count('Submitted'), icon: 'pending_actions' },
      { label: 'Approved', value: count('Approved'), icon: 'task_alt' },
      { label: 'In fulfilment', value: count('InFulfillment'), icon: 'inventory_2' },
      { label: 'Completed', value: count('Completed'), icon: 'check_circle' },
    ];
  });

  protected readonly recent = computed(() =>
    [...this.requests()].sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime()).slice(0, 5),
  );

  constructor() {
    this.service.loadAll();
  }

  protected refresh(): void {
    this.service.loadAll();
  }
}
