import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResultDto } from '../../shared/api/paged-result';
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
    targetIdShort: dto.targetId.slice(0, 8).toUpperCase(),
    fromStatus: dto.fromStatus,
    toStatus: dto.toStatus,
  };
}

/** Owns GET /audit-logs (read-only trail, Auditor/IT-Admin gated by the route). Server-paged: the
 *  trail can be large, so the screen pages through the API rather than loading it all. */
@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/audit-logs`;

  /** One-shot fetch of a page envelope (entries mapped + total/page/pageSize). */
  fetchPageResult(page?: number, pageSize?: number): Observable<PagedResultDto<AuditLogVm>> {
    let params = new HttpParams();
    if (page !== undefined) {
      params = params.set('page', page);
    }
    if (pageSize !== undefined) {
      params = params.set('pageSize', pageSize);
    }
    return this.http
      .get<PagedResultDto<AuditLogDto>>(this.baseUrl, { params })
      .pipe(map((res) => ({ ...res, items: res.items.map(mapAuditLog) })));
  }
}
