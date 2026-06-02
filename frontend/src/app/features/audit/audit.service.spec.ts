import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuditService, mapAuditLog } from './audit.service';
import { AuditLogDto, AuditLogVm } from './audit.models';
import { PagedResultDto } from '../../shared/api/paged-result';

function dto(overrides: Partial<AuditLogDto> = {}): AuditLogDto {
  return {
    id: 'a1',
    occurredAtUtc: '2026-05-01T10:00:00Z',
    actorId: 'user-1',
    action: 'RequestApproved',
    targetType: 'Request',
    targetId: 'req-1',
    fromStatus: 'Submitted',
    toStatus: 'Approved',
    ...overrides,
  };
}

describe('mapAuditLog', () => {
  it('resolves action metadata', () => {
    expect(mapAuditLog(dto()).actionMeta).toEqual({ label: 'Approved', tone: 'success' });
  });

  it('labels the reserved worker actor as System', () => {
    expect(mapAuditLog(dto({ actorId: 'ffffffff-ffff-ffff-ffff-ffffffffffff' })).actorLabel).toBe('System (worker)');
  });

  it('passes a human actor id through unchanged', () => {
    expect(mapAuditLog(dto()).actorLabel).toBe('user-1');
  });
});

describe('AuditService', () => {
  let service: AuditService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fetchPageResult GETs /api/audit-logs with paging and returns the mapped envelope', () => {
    let result: PagedResultDto<AuditLogVm> | undefined;
    service.fetchPageResult(2, 25).subscribe((res) => (result = res));
    const req = httpMock.expectOne((r) => r.url === '/api/audit-logs');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush({ items: [dto()], total: 12408, page: 2, pageSize: 25 });

    expect(result?.total).toBe(12408);
    expect(result?.items[0].action).toBe('RequestApproved');
  });
});
