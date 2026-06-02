import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  APPROVAL_DECISION_META,
  APPROVER_ROLE_LABEL,
  ApprovalStepDto,
  ApprovalStepVm,
  CreateRequestInput,
  PagedResultDto,
  REQUEST_CATEGORY_LABEL,
  REQUEST_PRIORITY_META,
  REQUEST_STATUS_META,
  REQUEST_TYPE_LABEL,
  RequestDto,
  RequestVm,
} from './requests.models';

/** Build the optional page/pageSize query params (only those provided are sent). Explicit
 *  undefined checks, not truthiness, so a literal 0 is still forwarded for the API to reject. */
function pageParams(page?: number, pageSize?: number): HttpParams {
  let params = new HttpParams();
  if (page !== undefined) {
    params = params.set('page', page);
  }
  if (pageSize !== undefined) {
    params = params.set('pageSize', pageSize);
  }
  return params;
}

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
    shortId: dto.id.slice(0, 8).toUpperCase(),
    title: dto.title,
    description: dto.description,
    type: dto.type,
    typeLabel: REQUEST_TYPE_LABEL[dto.type],
    priority: dto.priority,
    priorityMeta: REQUEST_PRIORITY_META[dto.priority],
    category: dto.category,
    categoryLabel: dto.category ? REQUEST_CATEGORY_LABEL[dto.category] : null,
    status: dto.status,
    statusMeta: REQUEST_STATUS_META[dto.status],
    requesterId: dto.requesterId,
    departmentId: dto.departmentId,
    createdAt: new Date(dto.createdAtUtc),
    fulfilledAssetId: dto.fulfilledAssetId,
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

  // The shared default-page list, used by the simple queue screens (Approvals, Fulfilment) that
  // just want "the requests". Screens that page (the list) own their own fetch via fetchPageResult
  // so their page selection never pollutes this shared signal.
  private readonly _requests = signal<readonly RequestVm[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly requests = this._requests.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  /** Load (or refresh) the default page into the shared signal. Ignored while a load is already in
   *  flight (single GET, no cancellation plumbing). For screens that just need the request list;
   *  paged screens use {@link fetchPageResult} so they don't share/clobber this signal. */
  loadAll(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    this.http
      .get<PagedResultDto<RequestDto>>(this.baseUrl)
      .pipe(map((res) => res.items.map(mapRequest)))
      .subscribe({
        next: (items) => {
          this._requests.set(items);
          this._loading.set(false);
        },
        error: () => {
          this._error.set(true);
          this._loading.set(false);
        },
      });
  }

  /** One-shot fetch of a page's items, mapped to view models, independent of the shared list signal
   *  and its single-flight guard. For callers that need their own snapshot regardless of what the
   *  list screen is doing (e.g. the dashboard's KPI/recent counts). `pageSize` overrides the default. */
  fetchPage(pageSize?: number): Observable<readonly RequestVm[]> {
    return this.http
      .get<PagedResultDto<RequestDto>>(this.baseUrl, { params: pageParams(undefined, pageSize) })
      .pipe(map((res) => res.items.map(mapRequest)));
  }

  /** One-shot fetch of a full page envelope (items mapped to view models, plus the total/page/
   *  pageSize for the footer), independent of the shared list signal. The paged list screen owns
   *  this so its page selection never clobbers the shared `requests` signal other screens read. */
  fetchPageResult(page?: number, pageSize?: number): Observable<PagedResultDto<RequestVm>> {
    return this.http
      .get<PagedResultDto<RequestDto>>(this.baseUrl, { params: pageParams(page, pageSize) })
      .pipe(map((res) => ({ ...res, items: res.items.map(mapRequest) })));
  }

  /** Fetch a single request by id (used by the detail screen). */
  getById(id: string): Observable<RequestVm> {
    return this.http.get<RequestDto>(`${this.baseUrl}/${id}`).pipe(map(mapRequest));
  }

  /** Create a Draft request. Requester + department are derived server-side from the JWT. */
  create(input: CreateRequestInput): Observable<RequestVm> {
    return this.http.post<RequestDto>(this.baseUrl, input).pipe(map(mapRequest));
  }

  /** Submit a Draft request for approval (the domain enforces submit-own + the stage). */
  submit(id: string): Observable<RequestVm> {
    return this.http.post<RequestDto>(`${this.baseUrl}/${id}/submit`, {}).pipe(map(mapRequest));
  }

  /** Cancel a request — the requester, while it's still Draft or Submitted (the API re-checks). */
  cancel(id: string): Observable<RequestVm> {
    return this.http.post<RequestDto>(`${this.baseUrl}/${id}/cancel`, {}).pipe(map(mapRequest));
  }

  /** Approve the current step — the actor's role + the configured chain select which step. */
  approve(id: string, note?: string): Observable<RequestVm> {
    return this.http
      .post<RequestDto>(`${this.baseUrl}/${id}/approve`, { note: note ?? null })
      .pipe(map(mapRequest));
  }

  /** Reject the current step with a required reason. */
  reject(id: string, reason: string): Observable<RequestVm> {
    return this.http.post<RequestDto>(`${this.baseUrl}/${id}/reject`, { reason }).pipe(map(mapRequest));
  }

  /** Fulfil an approved request (IT Admin) — Approved → Completed. An asset-lifecycle request is
   *  fulfilled by naming the in-stock asset to assign to the requester; other types send no asset. */
  fulfill(id: string, assignedAssetId?: string): Observable<RequestVm> {
    return this.http
      .post<RequestDto>(`${this.baseUrl}/${id}/fulfill`, { assignedAssetId: assignedAssetId ?? null })
      .pipe(map(mapRequest));
  }
}
