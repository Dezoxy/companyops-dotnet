import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { RequestsList } from './requests-list';
import { AuthService } from '../../../core/auth/auth.service';
import { RequestsService } from '../requests.service';
import { RequestVm } from '../requests.models';

function vm(title: string): RequestVm {
  return {
    id: `${title}-0000-0000-0000-000000000000`,
    shortId: title.slice(0, 8).toUpperCase(),
    title,
    description: null,
    type: 'Procurement',
    typeLabel: 'Procurement',
    priority: 'Medium',
    priorityMeta: { label: 'Medium', tone: 'info' },
    category: null,
    categoryLabel: null,
    status: 'Submitted',
    statusMeta: { label: 'Submitted', tone: 'info' },
    requesterId: 'r',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    fulfilledAssetId: null,
    approvalSteps: [],
  };
}

function fakeService(requests: RequestVm[], total = requests.length): RequestsService {
  return {
    // The list owns its own paged data via fetchPageResult (not the shared signal).
    fetchPageResult: () => of({ items: requests, total, page: 1, pageSize: 50 }),
    getById: () => of(requests[0]),
  } as unknown as RequestsService;
}

describe('RequestsList', () => {
  function setup(service: RequestsService, roles: string[] = ['Employee']) {
    const auth = { hasRole: (r: string) => roles.includes(r) } as unknown as AuthService;
    TestBed.configureTestingModule({
      imports: [RequestsList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: RequestsService, useValue: service },
        { provide: AuthService, useValue: auth },
      ],
    });
    return TestBed.createComponent(RequestsList);
  }

  it('shows the empty state when there are no requests', async () => {
    const fixture = setup(fakeService([], 0));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No requests yet.');
  });

  it('renders a row per request', async () => {
    const fixture = setup(fakeService([vm('alpha'), vm('beta')]));
    await fixture.whenStable();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.requests-table tbody tr');
    expect(rows.length).toBe(2);
  });

  it('shows the true total in the pagination footer', async () => {
    const fixture = setup(fakeService([vm('alpha'), vm('beta')], 142));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('of 142 results');
  });

  it('hides the New request action for roles that cannot create', async () => {
    const fixture = setup(fakeService([vm('alpha')]), ['Auditor']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('New request');
  });
});
