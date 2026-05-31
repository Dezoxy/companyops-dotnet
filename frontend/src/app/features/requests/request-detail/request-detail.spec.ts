import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { RequestDetail } from './request-detail';
import { RequestsService } from '../requests.service';
import { RequestVm } from '../requests.models';

function vm(): RequestVm {
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
  };
}

function setup(getById: () => Observable<RequestVm>) {
  TestBed.configureTestingModule({
    imports: [RequestDetail],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'abcdef12' }) } } },
      { provide: RequestsService, useValue: { getById } as unknown as RequestsService },
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
});
