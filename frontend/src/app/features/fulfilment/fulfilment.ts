import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';

import { RequestsService } from '../requests/requests.service';
import { RequestPriority, RequestType, RequestVm } from '../requests/requests.models';
import { StatusChip } from '../../shared/status-chip/status-chip';

interface StatTile {
  readonly label: string;
  readonly value: number;
  readonly icon: string;
}

// Critical first, then High / Medium / Low — IT works the queue top-down.
const PRIORITY_RANK: Record<RequestPriority, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };

/**
 * IT-Admin fulfilment console: the work queue of **Approved** requests awaiting fulfillment across
 * every flow (helpdesk / asset / procurement), ordered by priority then age, with at-a-glance
 * counts. Derived client-side from the shared `GET /requests` list — there is no per-queue endpoint
 * (same pattern as the Approvals screen). The actual fulfillment happens on the request detail
 * (including the asset picker for asset-lifecycle requests); this screen routes there. The API
 * enforces the `FulfillRequests` policy, so the route role-gate here is UX only.
 */
@Component({
  selector: 'app-fulfilment',
  imports: [
    DatePipe,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    StatusChip,
  ],
  templateUrl: './fulfilment.html',
  styleUrl: './fulfilment.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fulfilment {
  private readonly service = inject(RequestsService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly columns = ['title', 'type', 'priority', 'created', 'actions'];

  /** Approved requests, most urgent first (priority, then oldest). `Approved` is the
   *  fulfillment-ready state — fulfillment is synchronous (Approved → Completed), so a request
   *  never sits in `InFulfillment` today; that state is reserved for async worker fulfillment.
   *  `filter` copies, so the sort never mutates the service's signal array. */
  protected readonly queue = computed<readonly RequestVm[]>(() =>
    this.service
      .requests()
      .filter((request) => request.status === 'Approved')
      .sort(
        (a, b) => PRIORITY_RANK[a.priority] - PRIORITY_RANK[b.priority] || a.createdAt.getTime() - b.createdAt.getTime(),
      ),
  );

  protected readonly tiles = computed<readonly StatTile[]>(() => {
    const queue = this.queue();
    const byType = (type: RequestType) => queue.filter((request) => request.type === type).length;
    return [
      { label: 'Awaiting fulfilment', value: queue.length, icon: 'pending_actions' },
      { label: 'Helpdesk', value: byType('Helpdesk'), icon: 'support_agent' },
      { label: 'Asset requests', value: byType('AssetLifecycle'), icon: 'inventory_2' },
      { label: 'Procurement', value: byType('Procurement'), icon: 'shopping_cart' },
    ];
  });

  constructor() {
    this.service.loadAll();
  }

  protected refresh(): void {
    this.service.loadAll();
  }
}
