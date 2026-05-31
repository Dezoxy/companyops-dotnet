import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AUDIT_ACTION_META, AuditLogDto, AuditLogVm } from './audit.models';

/** The reserved system-worker actor (WellKnownActors.SystemWorker) — worker-driven audit
 *  entries have no human principal. */
const SYSTEM_ACTOR = 'ffffffff-ffff-ffff-ffff-ffffffffffff';

/** Map an audit DTO → view model (parsed date, action metadata, friendly system actor). */
export function mapAuditLog(dto: AuditLogDto): AuditLogVm {
  return {
    id: dto.id,
    occurredAt: new Date(dto.occurredAtUtc),
    actorLabel: dto.actorId === SYSTEM_ACTOR ? 'System (worker)' : dto.actorId,
    action: dto.action,
    // Fall back gracefully if the backend logs an action the frontend doesn't know yet
    // (e.g. a later phase adds one) rather than crashing the whole audit view.
    actionMeta: AUDIT_ACTION_META[dto.action] ?? { label: dto.action, tone: 'neutral' },
    targetType: dto.targetType,
    targetId: dto.targetId,
    fromStatus: dto.fromStatus,
    toStatus: dto.toStatus,
  };
}

/** Owns GET /audit-logs and exposes the (read-only) trail as signals. Auditor-gated by the route. */
@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/audit-logs`;

  private readonly _logs = signal<readonly AuditLogVm[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly logs = this._logs.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadAll(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    this.http.get<AuditLogDto[]>(this.baseUrl).pipe(map((dtos) => dtos.map(mapAuditLog))).subscribe({
      next: (logs) => {
        this._logs.set(logs);
        this._loading.set(false);
      },
      error: () => {
        this._error.set(true);
        this._loading.set(false);
      },
    });
  }
}
