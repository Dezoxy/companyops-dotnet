import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { RequestsList } from './requests-list';
import { RequestsService } from '../requests.service';
import { RequestVm } from '../requests.models';

function vm(title: string): RequestVm {
  return {
    id: `${title}-0000-0000-0000-000000000000`,
    title,
    description: null,
    type: 'Procurement',
    typeLabel: 'Procurement',
    status: 'Submitted',
    statusMeta: { label: 'Submitted', tone: 'info' },
    requesterId: 'r',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    approvalSteps: [],
  };
}

function fakeService(requests: RequestVm[]): RequestsService {
  return {
    requests: signal(requests),
    loading: signal(false),
    error: signal(false),
    loadAll: () => undefined,
    getById: () => of(requests[0]),
  } as unknown as RequestsService;
}

describe('RequestsList', () => {
  function setup(service: RequestsService) {
    TestBed.configureTestingModule({
      imports: [RequestsList],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: RequestsService, useValue: service }],
    });
    return TestBed.createComponent(RequestsList);
  }

  it('shows the empty state when there are no requests', async () => {
    const fixture = setup(fakeService([]));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No requests yet.');
  });

  it('renders a row per request', async () => {
    const fixture = setup(fakeService([vm('alpha'), vm('beta')]));
    await fixture.whenStable();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tr[mat-row]');
    expect(rows.length).toBe(2);
  });
});
