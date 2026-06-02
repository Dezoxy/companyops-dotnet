import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';

import { IntegrationsService } from './integrations.service';
import { StatusChip } from '../../shared/status-chip/status-chip';

/**
 * Integrations status: a read-only operational view of the async pipeline (outbox + worker,
 * ADR 0007/0008) — summary tiles plus the most recent messages with their relay status. Read from
 * `GET /integrations/status`; the route gates it to IT Admin / Auditor. The API is the authority.
 */
@Component({
  selector: 'app-integrations',
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusChip,
  ],
  templateUrl: './integrations.html',
  styleUrl: './integrations.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Integrations {
  private readonly service = inject(IntegrationsService);

  protected readonly status = this.service.status;
  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;

  protected readonly messages = computed(() => this.status()?.messages ?? []);

  constructor() {
    this.service.load();
  }

  protected refresh(): void {
    this.service.load();
  }
}
