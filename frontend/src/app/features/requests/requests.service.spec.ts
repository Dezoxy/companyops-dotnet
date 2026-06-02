import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { RequestsService, mapRequest } from './requests.service';
import { PagedResultDto, RequestDto, RequestVm } from './requests.models';

function dto(overrides: Partial<RequestDto> = {}): RequestDto {
  return {
    id: 'abcdef12-3456-7890-abcd-ef1234567890',
    title: 'New laptop',
    description: 'For onboarding',
    type: 'Procurement',
    priority: 'Medium',
    category: null,
    status: 'Submitted',
    requesterId: 'r-1',
    departmentId: 'd-1',
    createdAtUtc: '2026-05-01T10:00:00Z',
    fulfilledAssetId: null,
    approvalSteps: [
      { order: 2, requiredRole: 'Finance', scope: 'Global', isRequired: true, decision: 'Pending', decidedById: null, decidedAtUtc: null, note: null },
      { order: 1, requiredRole: 'Manager', scope: 'Department', isRequired: true, decision: 'Approved', decidedById: 'm-1', decidedAtUtc: '2026-05-02T10:00:00Z', note: 'Looks good' },
    ],
    ...overrides,
  };
}

describe('mapRequest', () => {
  it('passes the request id through unchanged', () => {
    expect(mapRequest(dto()).id).toBe('abcdef12-3456-7890-abcd-ef1234567890');
  });

  it('resolves status display metadata', () => {
    const vm = mapRequest(dto({ status: 'InFulfillment' }));
    expect(vm.statusMeta).toEqual({ label: 'In fulfilment', tone: 'progress' });
  });

  it('orders approval steps and flags the first pending one as current', () => {
    const vm = mapRequest(dto());
    expect(vm.approvalSteps.map((s) => s.order)).toEqual([1, 2]);
    expect(vm.approvalSteps[0].isCurrent).toBe(false); // Manager, already approved
    expect(vm.approvalSteps[1].isCurrent).toBe(true); // Finance, pending
  });

  it('parses decision timestamps to dates', () => {
    const manager = mapRequest(dto()).approvalSteps[0];
    expect(manager.decidedAt).toEqual(new Date('2026-05-02T10:00:00Z'));
    expect(manager.roleLabel).toBe('Manager');
  });

  it('passes the fulfilled asset link through', () => {
    expect(mapRequest(dto({ fulfilledAssetId: 'asset-9' })).fulfilledAssetId).toBe('asset-9');
    expect(mapRequest(dto()).fulfilledAssetId).toBeNull();
  });
});

describe('RequestsService', () => {
  let service: RequestsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RequestsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadAll GETs the default page and populates the shared items signal', () => {
    service.loadAll();
    const req = httpMock.expectOne('/api/requests');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [dto()], total: 142, page: 1, pageSize: 50 });

    expect(service.loading()).toBe(false);
    expect(service.error()).toBe(false);
    expect(service.requests()).toHaveLength(1);
    expect(service.requests()[0].title).toBe('New laptop');
  });

  it('fetchPageResult passes 1-based page + pageSize and returns the mapped envelope', () => {
    let result: PagedResultDto<RequestVm> | undefined;
    service.fetchPageResult(2, 25).subscribe((res) => (result = res));
    const req = httpMock.expectOne((r) => r.url === '/api/requests');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush({ items: [dto()], total: 142, page: 2, pageSize: 25 });
    expect(result?.total).toBe(142);
    expect(result?.items.map((i) => i.title)).toEqual(['New laptop']);
  });

  it('fetchPage returns just the mapped items for the given page size', () => {
    let titles: string[] | undefined;
    service.fetchPage(200).subscribe((items) => (titles = items.map((i) => i.title)));
    const req = httpMock.expectOne((r) => r.url === '/api/requests');
    expect(req.request.params.get('pageSize')).toBe('200');
    req.flush({ items: [dto()], total: 1, page: 1, pageSize: 200 });
    expect(titles).toEqual(['New laptop']);
  });

  it('loadAll sets the error signal on failure', () => {
    service.loadAll();
    httpMock.expectOne('/api/requests').flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.loading()).toBe(false);
    expect(service.error()).toBe(true);
  });

  it('getById GETs the single request and maps it', () => {
    let result: string | undefined;
    service.getById('abcdef12-3456-7890-abcd-ef1234567890').subscribe((r) => (result = r.title));
    httpMock.expectOne('/api/requests/abcdef12-3456-7890-abcd-ef1234567890').flush(dto());
    expect(result).toBe('New laptop');
  });

  it('create POSTs the input and maps the response', () => {
    let result: string | undefined;
    service
      .create({ title: 'New laptop', type: 'Procurement', description: null, priority: 'Medium' })
      .subscribe((r) => (result = r.id));
    const req = httpMock.expectOne('/api/requests');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'New laptop', type: 'Procurement', description: null, priority: 'Medium' });
    req.flush(dto());
    expect(result).toBe('abcdef12-3456-7890-abcd-ef1234567890');
  });

  it('submit POSTs to the submit endpoint', () => {
    service.submit('r1').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/submit');
    expect(req.request.method).toBe('POST');
    req.flush(dto());
  });

  it('cancel POSTs to the cancel endpoint', () => {
    service.cancel('r1').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/cancel');
    expect(req.request.method).toBe('POST');
    req.flush(dto());
  });

  it('approve POSTs the note to the approve endpoint', () => {
    service.approve('r1', 'looks good').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/approve');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ note: 'looks good' });
    req.flush(dto());
  });

  it('reject POSTs the reason to the reject endpoint', () => {
    service.reject('r1', 'over budget').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'over budget' });
    req.flush(dto());
  });

  it('fulfill POSTs a null asset id for non-asset requests', () => {
    service.fulfill('r1').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/fulfill');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ assignedAssetId: null });
    req.flush(dto());
  });

  it('fulfill POSTs the chosen asset id for an asset-lifecycle request', () => {
    service.fulfill('r1', 'asset-7').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/fulfill');
    expect(req.request.body).toEqual({ assignedAssetId: 'asset-7' });
    req.flush(dto({ fulfilledAssetId: 'asset-7' }));
  });
});
