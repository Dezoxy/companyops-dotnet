import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { Dashboard } from './dashboard';
import { RequestsService } from '../requests/requests.service';
import { RequestStatus, RequestVm } from '../requests/requests.models';

function vm(status: RequestStatus): RequestVm {
  return {
    id: `${status}-0000-0000-0000-000000000000`,
    title: `${status} request`,
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

describe('Dashboard', () => {
  it('counts requests by status into the stat cards', async () => {
    const service = {
      requests: signal([vm('Submitted'), vm('Submitted'), vm('Completed'), vm('Rejected')]),
      loading: signal(false),
      error: signal(false),
      loadAll: () => undefined,
      getById: () => of(vm('Submitted')),
    } as unknown as RequestsService;

    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: RequestsService, useValue: service }],
    });
    const fixture = TestBed.createComponent(Dashboard);
    await fixture.whenStable();

    const values = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.stat-value'),
    ).map((el) => el.textContent?.trim());
    // Order: Awaiting approval (Submitted), Approved, Rejected, Completed
    expect(values).toEqual(['2', '0', '1', '1']);
  });
});
