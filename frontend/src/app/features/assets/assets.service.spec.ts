import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AssetsService, mapAsset } from './assets.service';
import { AssetDto } from './assets.models';

function dto(overrides: Partial<AssetDto> = {}): AssetDto {
  return {
    id: 'a1',
    tag: 'AST-1',
    name: 'MacBook',
    type: 'Laptop',
    status: 'InStock',
    assignedToId: null,
    createdAtUtc: '2026-05-01T10:00:00Z',
    ...overrides,
  };
}

describe('mapAsset', () => {
  it('resolves status metadata and type label', () => {
    const vm = mapAsset(dto({ status: 'Assigned' }));
    expect(vm.statusMeta).toEqual({ label: 'Assigned', tone: 'success' });
    expect(vm.typeLabel).toBe('Laptop');
  });
});

describe('AssetsService', () => {
  let service: AssetsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AssetsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadAll GETs /api/assets and populates the signal', () => {
    service.loadAll();
    const req = httpMock.expectOne('/api/assets');
    expect(req.request.method).toBe('GET');
    req.flush([dto()]);
    expect(service.assets()).toHaveLength(1);
    expect(service.loading()).toBe(false);
  });

  it('register POSTs the input', () => {
    service.register({ tag: 'AST-2', name: 'Dell', type: 'Mobile' }).subscribe();
    const req = httpMock.expectOne('/api/assets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ tag: 'AST-2', name: 'Dell', type: 'Mobile' });
    req.flush(dto());
  });

  it('assign POSTs the userId', () => {
    service.assign('a1', 'u1').subscribe();
    const req = httpMock.expectOne('/api/assets/a1/assign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'u1' });
    req.flush(dto());
  });

  it('reclaim POSTs to the reclaim endpoint', () => {
    service.reclaim('a1').subscribe();
    const req = httpMock.expectOne('/api/assets/a1/reclaim');
    expect(req.request.method).toBe('POST');
    req.flush(dto());
  });

  it('history GETs the asset history', () => {
    let count: number | undefined;
    service.history('a1').subscribe((h) => (count = h.length));
    const req = httpMock.expectOne('/api/assets/a1/history');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'h1', occurredAtUtc: '2026-05-01T10:00:00Z', actorId: 'x', action: 'AssetRegistered', fromStatus: null, toStatus: 'InStock' }]);
    expect(count).toBe(1);
  });
});
