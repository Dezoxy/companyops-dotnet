import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssetReportDto,
  ReportVm,
  RequestReportDto,
  mapAssetReport,
  mapRequestReport,
} from './reports.models';

/**
 * Owns the `/reports` HTTP calls and exposes the two reports as signals (with loading/error).
 * Both endpoints are fetched together (`forkJoin`) so the screen renders in one pass. DTOs are
 * mapped to breakdown view models here — components never touch raw HTTP shapes (frontend/CLAUDE.md).
 */
@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reports`;

  private readonly _requestReport = signal<ReportVm | null>(null);
  private readonly _assetReport = signal<ReportVm | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal(false);

  readonly requestReport = this._requestReport.asReadonly();
  readonly assetReport = this._assetReport.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  /** Load (or refresh) both reports. Ignored while a load is already in flight. */
  load(): void {
    if (this._loading()) {
      return;
    }
    this._loading.set(true);
    this._error.set(false);
    forkJoin({
      requests: this.http.get<RequestReportDto>(`${this.baseUrl}/requests`),
      assets: this.http.get<AssetReportDto>(`${this.baseUrl}/assets`),
    }).subscribe({
      next: ({ requests, assets }) => {
        this._requestReport.set(mapRequestReport(requests));
        this._assetReport.set(mapAssetReport(assets));
        this._loading.set(false);
      },
      error: () => {
        this._error.set(true);
        this._loading.set(false);
      },
    });
  }
}
