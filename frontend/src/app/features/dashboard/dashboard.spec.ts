import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { Dashboard } from './dashboard';
import { RequestsService } from '../requests/requests.service';
import { ReportsService } from '../reports/reports.service';
import { IntegrationsService } from '../integrations/integrations.service';
import { RequestStatus, RequestVm } from '../requests/requests.models';
import { KpiCounts, ReportVm } from '../reports/reports.models';
import { IntegrationStatusVm } from '../integrations/integrations.models';

function vm(status: RequestStatus, title: string): RequestVm {
  return {
    id: `${status}-0000-0000-0000-000000000000`,
    title,
    description: null,
    type: 'Procurement',
    typeLabel: 'Procurement',
    priority: 'Medium',
    priorityMeta: { label: 'Medium', tone: 'info' },
    category: null,
    categoryLabel: null,
    status,
    statusMeta: { label: status, tone: 'info' },
    requesterId: 'r',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    fulfilledAssetId: null,
    approvalSteps: [],
  };
}

const kpiCounts: KpiCounts = {
  total: 10,
  byStatus: { Submitted: 3, Approved: 2, Completed: 4, Rejected: 1 },
  byPriority: { Medium: 8, Critical: 2 },
};

const assetReport: ReportVm = { title: 'Assets', total: 1240, sections: [] };

const systemStatus: IntegrationStatusVm = {
  tiles: [
    { label: 'Total', value: 5, icon: 'all_inbox' },
    { label: 'Pending', value: 0, icon: 'schedule' },
    { label: 'Published', value: 5, icon: 'cloud_done' },
    { label: 'Failed', value: 0, icon: 'error_outline' },
    { label: 'Processed by worker', value: 5, icon: 'done_all' },
  ],
  messages: [],
};

function render(reqs: RequestVm[]) {
  const requestsService = {
    requests: signal(reqs),
    loading: signal(false),
    error: signal(false),
    loadAll: () => undefined,
    getById: () => of(reqs[0]),
  } as unknown as RequestsService;

  const reportsService = {
    kpiCounts: signal<KpiCounts | null>(kpiCounts),
    assetReport: signal<ReportVm | null>(assetReport),
    requestReport: signal<ReportVm | null>(null),
    loading: signal(false),
    error: signal(false),
    load: () => undefined,
  } as unknown as ReportsService;

  const integrationsService = {
    status: signal<IntegrationStatusVm | null>(systemStatus),
    loading: signal(false),
    error: signal(false),
    load: () => undefined,
  } as unknown as IntegrationsService;

  TestBed.configureTestingModule({
    imports: [Dashboard],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: RequestsService, useValue: requestsService },
      { provide: ReportsService, useValue: reportsService },
      { provide: IntegrationsService, useValue: integrationsService },
    ],
  });
  return TestBed.createComponent(Dashboard);
}

describe('Dashboard', () => {
  it('derives the KPI cards from the reports aggregates', async () => {
    const fixture = render([vm('Submitted', 'A')]);
    await fixture.whenStable();

    const values = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.stat-value'),
    ).map((el) => el.textContent?.trim());
    // active = total(10) - terminal(Completed 4 + Rejected 1) = 5; pending = Submitted 3;
    // critical = priority Critical 2; managed assets = asset report total 1240.
    expect(values).toEqual(['5', '3', '2', '1240']);
  });

  it('lists the most recent requests in the activity table', async () => {
    const fixture = render([vm('Submitted', 'First request'), vm('Approved', 'Second request')]);
    await fixture.whenStable();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.activity tbody tr');
    expect(rows.length).toBe(2);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('First request');
  });

  it('renders a system-status row per derived service', async () => {
    const fixture = render([vm('Submitted', 'A')]);
    await fixture.whenStable();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.status-list li');
    expect(rows.length).toBe(3);
  });
});
