import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { Reports } from './reports';
import { ReportsService } from './reports.service';
import {
  AssetReportDto,
  KpiCounts,
  ReportVm,
  RequestReportDto,
  mapAssetReport,
  mapRequestReport,
} from './reports.models';
import { REQUEST_PRIORITY_META, REQUEST_STATUS_META, REQUEST_TYPE_LABEL } from '../requests/requests.models';
import { ASSET_STATUS_META } from '../assets/assets.models';

describe('report mappers', () => {
  it('maps request buckets to labels, tones, and percent of total', () => {
    const dto: RequestReportDto = {
      total: 10,
      byStatus: [
        { key: 'Approved', count: 6 },
        { key: 'Draft', count: 4 },
      ],
      byType: [{ key: 'Procurement', count: 10 }],
      byPriority: [{ key: 'Critical', count: 10 }],
    };

    const vm = mapRequestReport(dto);

    expect(vm.title).toBe('Requests');
    expect(vm.total).toBe(10);
    expect(vm.sections[0].title).toBe('By status');
    expect(vm.sections[0].rows[0]).toEqual({
      label: REQUEST_STATUS_META.Approved.label,
      count: 6,
      percent: 60,
      tone: REQUEST_STATUS_META.Approved.tone,
    });
    expect(vm.sections[0].rows[1].percent).toBe(40);
    // Type buckets carry no status tone — rendered in the neutral 'info' accent.
    expect(vm.sections[1].rows[0]).toEqual({ label: REQUEST_TYPE_LABEL.Procurement, count: 10, percent: 100, tone: 'info' });
    expect(vm.sections[2].rows[0].tone).toBe(REQUEST_PRIORITY_META.Critical.tone);
  });

  it('rounds percent and never divides by zero', () => {
    const populated: AssetReportDto = {
      total: 3,
      byStatus: [{ key: 'InStock', count: 1 }],
      byType: [{ key: 'Laptop', count: 3 }],
    };
    expect(mapAssetReport(populated).sections[0].rows[0]).toEqual({
      label: ASSET_STATUS_META.InStock.label,
      count: 1,
      percent: 33,
      tone: ASSET_STATUS_META.InStock.tone,
    });

    const empty: AssetReportDto = { total: 0, byStatus: [], byType: [] };
    const vm = mapAssetReport(empty);
    expect(vm.total).toBe(0);
    expect(vm.sections[0].rows).toEqual([]);
  });
});

function fakeService(
  requestReport: ReportVm | null,
  assetReport: ReportVm | null,
  opts: { loading?: boolean; error?: boolean } = {},
): ReportsService {
  const kpiCounts: KpiCounts | null = requestReport
    ? { total: requestReport.total, byStatus: {}, byPriority: {} }
    : null;
  return {
    requestReport: signal(requestReport),
    assetReport: signal(assetReport),
    kpiCounts: signal(kpiCounts),
    loading: signal(opts.loading ?? false),
    error: signal(opts.error ?? false),
    load: () => undefined,
  } as unknown as ReportsService;
}

function setup(service: ReportsService) {
  TestBed.configureTestingModule({
    imports: [Reports],
    providers: [provideNoopAnimations(), { provide: ReportsService, useValue: service }],
  });
  return TestBed.createComponent(Reports);
}

describe('Reports', () => {
  it('renders breakdown bars for both reports', async () => {
    const requests = mapRequestReport({
      total: 2,
      byStatus: [{ key: 'Approved', count: 2 }],
      byType: [{ key: 'Helpdesk', count: 2 }],
      byPriority: [{ key: 'Medium', count: 2 }],
    });
    const assets = mapAssetReport({ total: 1, byStatus: [{ key: 'InStock', count: 1 }], byType: [{ key: 'Laptop', count: 1 }] });
    const fixture = setup(fakeService(requests, assets));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Requests');
    expect(el.textContent).toContain('Assets');
    expect(el.querySelectorAll('.bar-fill').length).toBeGreaterThan(0);
    // KPI summary cards render once the counts are loaded.
    expect(el.querySelectorAll('.kpi-card').length).toBe(4);
  });

  it('shows a per-report empty state when a report has no data', async () => {
    const fixture = setup(
      fakeService(
        mapRequestReport({ total: 0, byStatus: [], byType: [], byPriority: [] }),
        mapAssetReport({ total: 0, byStatus: [], byType: [] }),
      ),
    );
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No data yet.');
  });

  it('shows the error state instead of any bars', async () => {
    const fixture = setup(fakeService(null, null, { error: true }));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain("Couldn't load reports.");
    expect(el.querySelector('.bar-fill')).toBeNull();
    expect(el.querySelector('.kpi-grid')).toBeNull(); // no KPI cards without data
  });
});
