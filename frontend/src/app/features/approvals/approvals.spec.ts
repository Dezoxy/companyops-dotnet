import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { Approvals } from './approvals';
import { RequestsService } from '../requests/requests.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApproverRole, RequestStatus, RequestVm } from '../requests/requests.models';

function vm(title: string, status: RequestStatus, currentRole: ApproverRole | null): RequestVm {
  return {
    id: title,
    shortId: title.slice(0, 8).toUpperCase(),
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
    approvalSteps: currentRole
      ? [
          {
            order: 1,
            requiredRole: currentRole,
            roleLabel: currentRole,
            scope: 'Global',
            isRequired: true,
            decision: 'Pending',
            decisionMeta: { label: 'Pending', tone: 'neutral' },
            decidedById: null,
            decidedAt: null,
            note: null,
            isCurrent: true,
          },
        ]
      : [],
  };
}

function setup(requests: RequestVm[], roles: string[]) {
  const service = {
    requests: signal(requests),
    loading: signal(false),
    error: signal(false),
    loadAll: () => undefined,
  } as unknown as RequestsService;
  const auth = { roles: () => roles, hasRole: (r: string) => roles.includes(r) } as unknown as AuthService;

  TestBed.configureTestingModule({
    imports: [Approvals],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: RequestsService, useValue: service },
      { provide: AuthService, useValue: auth },
    ],
  });
  return TestBed.createComponent(Approvals);
}

describe('Approvals', () => {
  it('lists only submitted requests whose current step matches my role', async () => {
    const fixture = setup(
      [vm('mine', 'Submitted', 'Manager'), vm('finance-step', 'Submitted', 'Finance'), vm('draft', 'Draft', null)],
      ['Manager'],
    );
    await fixture.whenStable();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.approvals-table tbody tr');
    expect(rows.length).toBe(1);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('mine');
  });

  it('shows an empty state when nothing awaits my decision', async () => {
    const fixture = setup([vm('finance-step', 'Submitted', 'Finance')], ['Manager']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nothing is awaiting your decision.');
  });
});
