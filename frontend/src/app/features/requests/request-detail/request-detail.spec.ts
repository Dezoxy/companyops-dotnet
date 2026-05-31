import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { RequestDetail } from './request-detail';
import { RequestsService } from '../requests.service';
import { ApprovalStepVm, ApproverRole, RequestVm } from '../requests.models';
import { AuthService } from '../../../core/auth/auth.service';

const fakeAuth = {
  userId: () => 'u',
  roles: () => [],
  hasRole: () => false,
} as unknown as AuthService;

function auth(userId: string, roles: string[]): AuthService {
  return { userId: () => userId, roles: () => roles, hasRole: (r: string) => roles.includes(r) } as unknown as AuthService;
}

function step(role: ApproverRole): ApprovalStepVm {
  return {
    order: 1,
    requiredRole: role,
    roleLabel: role,
    scope: 'Global',
    isRequired: true,
    decision: 'Pending',
    decisionMeta: { label: 'Pending', tone: 'neutral' },
    decidedById: null,
    decidedAt: null,
    note: null,
    isCurrent: true,
  };
}

function vm(overrides: Partial<RequestVm> = {}): RequestVm {
  return {
    id: 'abcdef12-0000-0000-0000-000000000000',
    title: 'New laptop',
    description: 'For onboarding',
    type: 'Procurement',
    typeLabel: 'Procurement',
    status: 'Submitted',
    statusMeta: { label: 'Submitted', tone: 'info' },
    requesterId: 'r',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    approvalSteps: [],
    ...overrides,
  };
}

function setup(getById: () => Observable<RequestVm>, authService: AuthService = fakeAuth) {
  TestBed.configureTestingModule({
    imports: [RequestDetail],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'abcdef12' }) } } },
      { provide: RequestsService, useValue: { getById } as unknown as RequestsService },
      { provide: AuthService, useValue: authService },
    ],
  });
  return TestBed.createComponent(RequestDetail);
}

describe('RequestDetail', () => {
  it('renders the request when loaded', async () => {
    const fixture = setup(() => of(vm()));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New laptop');
  });

  it('shows a not-found state on 404', async () => {
    const fixture = setup(() => throwError(() => new HttpErrorResponse({ status: 404 })));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain("doesn't exist");
  });

  it('shows Submit for the owner of a draft', async () => {
    const fixture = setup(() => of(vm({ status: 'Draft', requesterId: 'me' })), auth('me', []));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Submit for approval');
  });

  it('hides Submit for a draft the user does not own', async () => {
    const fixture = setup(() => of(vm({ status: 'Draft', requesterId: 'me' })), auth('someone-else', []));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Submit for approval');
  });

  it('shows Approve/Reject when the current step matches a role the user holds', async () => {
    const fixture = setup(() => of(vm({ status: 'Submitted', approvalSteps: [step('Manager')] })), auth('x', ['Manager']));
    await fixture.whenStable();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Approve');
    expect(text).toContain('Reject');
  });

  it('hides Approve/Reject when the step role is not held', async () => {
    const fixture = setup(() => of(vm({ status: 'Submitted', approvalSteps: [step('Finance')] })), auth('x', ['Manager']));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Approve');
  });

  it('shows Mark fulfilled for IT Admin on an approved request', async () => {
    const fixture = setup(() => of(vm({ status: 'Approved' })), auth('x', ['ItAdmin']));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Mark fulfilled');
  });

  it('hides Mark fulfilled for a non-IT-Admin on an approved request', async () => {
    const fixture = setup(() => of(vm({ status: 'Approved' })), auth('x', ['Manager']));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Mark fulfilled');
  });

  it('hides Mark fulfilled for IT Admin until the request is approved', async () => {
    const fixture = setup(() => of(vm({ status: 'Submitted' })), auth('x', ['ItAdmin']));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Mark fulfilled');
  });
});
