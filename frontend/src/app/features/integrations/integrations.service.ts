import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { IntegrationStatusDto, IntegrationStatusVm, mapIntegrationStatus } from './integrations.models';

/**
 * Owns the `/integrations` HTTP and exposes the pipeline snapshot as signals (with loading/error).
 * The DTO is mapped to a view model here — components stay presentational (frontend/CLAUDE.md).
 */
@Injectable({ providedIn: 'root' })
export class IntegrationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/integrations`;

  private readonly _status = signal<IntegrationStatusVm | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly status = this._status.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  /** Load (or refresh) the integration status. Ignored while a load is already in flight. */
  load(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    this.http
      .get<IntegrationStatusDto>(`${this.baseUrl}/status`)
      .pipe(map(mapIntegrationStatus))
      .subscribe({
        next: (status) => {
          this._status.set(status);
          this._loading.set(false);
        },
        error: () => {
          this._error.set(true);
          this._loading.set(false);
        },
      });
  }
}
