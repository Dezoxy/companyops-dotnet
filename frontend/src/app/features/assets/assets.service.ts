import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResultDto } from '../../shared/api/paged-result';
import {
  ASSET_ACTION_LABEL,
  ASSET_STATUS_META,
  ASSET_TYPE_LABEL,
  AssetDto,
  AssetHistoryDto,
  AssetHistoryVm,
  AssetVm,
  RegisterAssetInput,
} from './assets.models';

// Mirrors the server's PageRequest.MaxPageSize — the shared inventory load pulls this many.
const MAX_PAGE_SIZE = 200;

export function mapAsset(dto: AssetDto): AssetVm {
  return {
    id: dto.id,
    tag: dto.tag,
    name: dto.name,
    type: dto.type,
    typeLabel: ASSET_TYPE_LABEL[dto.type],
    status: dto.status,
    statusMeta: ASSET_STATUS_META[dto.status],
    assignedToId: dto.assignedToId,
    assignedToIdShort: dto.assignedToId ? dto.assignedToId.slice(0, 8).toUpperCase() : null,
    createdAt: new Date(dto.createdAtUtc),
  };
}

export function mapHistory(dto: AssetHistoryDto): AssetHistoryVm {
  return {
    id: dto.id,
    occurredAt: new Date(dto.occurredAtUtc),
    actorId: dto.actorId,
    actionLabel: ASSET_ACTION_LABEL[dto.action] ?? dto.action,
    fromStatus: dto.fromStatus,
    toStatus: dto.toStatus,
  };
}

/**
 * Owns all `/assets` HTTP. Exposes the inventory as signals (with loading/error); detail,
 * history, and the lifecycle actions return observables. DTOs are mapped to view models here.
 * The token is attached by the global interceptor.
 */
@Injectable({ providedIn: 'root' })
export class AssetsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/assets`;

  private readonly _assets = signal<readonly AssetVm[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly assets = this._assets.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  /** Load the inventory into the shared signal — used by callers that just need the asset list
   *  (e.g. the fulfil-asset dialog picking an in-stock asset). Requests the server's max page so
   *  the list isn't silently truncated to the default page; the paged list owns its own fetch. */
  loadAll(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    this.http
      .get<PagedResultDto<AssetDto>>(this.baseUrl, { params: new HttpParams().set('pageSize', MAX_PAGE_SIZE) })
      .pipe(map((res) => res.items.map(mapAsset)))
      .subscribe({
        next: (assets) => {
          this._assets.set(assets);
          this._loading.set(false);
        },
        error: () => {
          this._error.set(true);
          this._loading.set(false);
        },
      });
  }

  /** One-shot fetch of a page envelope (items mapped + total/page/pageSize), independent of the
   *  shared signal — for the inventory list's server-side pagination. */
  fetchPageResult(page?: number, pageSize?: number): Observable<PagedResultDto<AssetVm>> {
    let params = new HttpParams();
    if (page !== undefined) {
      params = params.set('page', page);
    }
    if (pageSize !== undefined) {
      params = params.set('pageSize', pageSize);
    }
    return this.http
      .get<PagedResultDto<AssetDto>>(this.baseUrl, { params })
      .pipe(map((res) => ({ ...res, items: res.items.map(mapAsset) })));
  }

  getById(id: string): Observable<AssetVm> {
    return this.http.get<AssetDto>(`${this.baseUrl}/${id}`).pipe(map(mapAsset));
  }

  history(id: string): Observable<AssetHistoryVm[]> {
    return this.http.get<AssetHistoryDto[]>(`${this.baseUrl}/${id}/history`).pipe(map((dtos) => dtos.map(mapHistory)));
  }

  register(input: RegisterAssetInput): Observable<AssetVm> {
    return this.http.post<AssetDto>(this.baseUrl, input).pipe(map(mapAsset));
  }

  assign(id: string, userId: string): Observable<AssetVm> {
    return this.http.post<AssetDto>(`${this.baseUrl}/${id}/assign`, { userId }).pipe(map(mapAsset));
  }

  reclaim(id: string): Observable<AssetVm> {
    return this.transition(id, 'reclaim');
  }

  sendToRepair(id: string): Observable<AssetVm> {
    return this.transition(id, 'repair');
  }

  returnFromRepair(id: string): Observable<AssetVm> {
    return this.transition(id, 'return-from-repair');
  }

  retire(id: string): Observable<AssetVm> {
    return this.transition(id, 'retire');
  }

  private transition(id: string, verb: string): Observable<AssetVm> {
    return this.http.post<AssetDto>(`${this.baseUrl}/${id}/${verb}`, {}).pipe(map(mapAsset));
  }
}
