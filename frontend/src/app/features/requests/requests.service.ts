import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  APPROVAL_DECISION_META,
  APPROVER_ROLE_LABEL,
  ApprovalStepDto,
  ApprovalStepVm,
  REQUEST_STATUS_META,
  REQUEST_TYPE_LABEL,
  RequestDto,
  RequestVm,
} from './requests.models';

/** Map one API step DTO → view model. `isCurrent` is set by the caller (it depends on the
 *  whole chain — the first pending step). Exported for unit testing. */
export function mapApprovalStep(dto: ApprovalStepDto, isCurrent: boolean): ApprovalStepVm {
  return {
    order: dto.order,
    requiredRole: dto.requiredRole,
    roleLabel: APPROVER_ROLE_LABEL[dto.requiredRole],
    scope: dto.scope,
    isRequired: dto.isRequired,
    decision: dto.decision,
    decisionMeta: APPROVAL_DECISION_META[dto.decision],
    decidedById: dto.decidedById,
    decidedAt: dto.decidedAtUtc ? new Date(dto.decidedAtUtc) : null,
    note: dto.note,
    isCurrent,
  };
}

/** Map an API request DTO → view model (labels, tones, parsed dates resolved once). */
export function mapRequest(dto: RequestDto): RequestVm {
  const steps = [...dto.approvalSteps].sort((a, b) => a.order - b.order);
  const currentOrder = steps.find((s) => s.decision === 'Pending')?.order ?? null;
  return {
    id: dto.id,
    title: dto.title,
    description: dto.description,
    type: dto.type,
    typeLabel: REQUEST_TYPE_LABEL[dto.type],
    status: dto.status,
    statusMeta: REQUEST_STATUS_META[dto.status],
    requesterId: dto.requesterId,
    departmentId: dto.departmentId,
    createdAt: new Date(dto.createdAtUtc),
    approvalSteps: steps.map((s) => mapApprovalStep(s, s.order === currentOrder)),
  };
}

/**
 * Owns all `/requests` HTTP and exposes the list as signals (with loading/error). The token is
 * attached by the global interceptor — never per call. DTOs are mapped to view models here so
 * components never touch raw HTTP shapes (frontend/CLAUDE.md).
 */
@Injectable({ providedIn: 'root' })
export class RequestsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/requests`;

  private readonly _requests = signal<readonly RequestVm[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly requests = this._requests.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  /** Load (or refresh) the full request list into the signals. Ignored while a load is already
   *  in flight — two consumers (dashboard + list) call this on init, and the refresh button is
   *  disabled while loading — so this keeps it to a single GET without cancellation plumbing. */
  loadAll(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    this.http.get<RequestDto[]>(this.baseUrl).pipe(map((dtos) => dtos.map(mapRequest))).subscribe({
      next: (requests) => {
        this._requests.set(requests);
        this._loading.set(false);
      },
      error: () => {
        this._error.set(true);
        this._loading.set(false);
      },
    });
  }

  /** Fetch a single request by id (used by the detail screen). */
  getById(id: string): Observable<RequestVm> {
    return this.http.get<RequestDto>(`${this.baseUrl}/${id}`).pipe(map(mapRequest));
  }
}
