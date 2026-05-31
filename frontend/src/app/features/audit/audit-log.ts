import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuditService } from './audit.service';
import { AUDIT_ACTION_META, AuditAction } from './audit.models';
import { StatusChip } from '../../shared/status-chip/status-chip';

/** Read-only audit trail (GET /audit-logs, Auditor-gated). Action filter is client-side over the
 *  loaded entries. The API has no write path — the log is append-only by design. */
@Component({
  selector: 'app-audit-log',
  imports: [
    DatePipe,
    MatTableModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusChip,
  ],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditLog {
  private readonly service = inject(AuditService);

  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly columns = ['occurredAt', 'action', 'target', 'actor', 'change'];

  protected readonly actionOptions = Object.entries(AUDIT_ACTION_META).map(([value, meta]) => ({
    value: value as AuditAction,
    label: meta.label,
  }));

  protected readonly actionFilter = signal<AuditAction | 'all'>('all');

  protected readonly filtered = computed(() => {
    const action = this.actionFilter();
    return this.service.logs().filter((log) => action === 'all' || log.action === action);
  });

  constructor() {
    this.service.loadAll();
  }

  protected onAction(value: AuditAction | 'all'): void {
    this.actionFilter.set(value);
  }

  protected refresh(): void {
    this.service.loadAll();
  }
}
