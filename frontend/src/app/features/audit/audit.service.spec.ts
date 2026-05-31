import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuditService, mapAuditLog } from './audit.service';
import { AuditLogDto } from './audit.models';

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

  it('loadAll GETs /api/audit-logs and populates the signal', () => {
    service.loadAll();
    const req = httpMock.expectOne('/api/audit-logs');
    expect(req.request.method).toBe('GET');
    req.flush([dto()]);

    expect(service.logs()).toHaveLength(1);
    expect(service.logs()[0].action).toBe('RequestApproved');
    expect(service.loading()).toBe(false);
  });
});
