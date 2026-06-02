import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { AssetDetail } from './asset-detail';
import { AssetsService } from '../assets.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AssetStatus, AssetVm } from '../assets.models';

function vm(status: AssetStatus): AssetVm {
  return {
    id: 'a1',
    tag: 'AST-1',
    name: 'MacBook Pro',
    type: 'Laptop',
    typeLabel: 'Laptop',
    status,
    statusMeta: { label: status, tone: 'info' },
    assignedToId: null,
    assignedToIdShort: null,
    createdAt: new Date('2026-05-01T00:00:00Z'),
  };
}

function setup(status: AssetStatus, roles: string[]) {
  const service = { getById: () => of(vm(status)), history: () => of([]) } as unknown as AssetsService;
  const auth = { hasRole: (r: string) => roles.includes(r) } as unknown as AuthService;

  TestBed.configureTestingModule({
    imports: [AssetDetail],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'a1' }) } } },
      { provide: AssetsService, useValue: service },
      { provide: AuthService, useValue: auth },
    ],
  });
  return TestBed.createComponent(AssetDetail);
}

describe('AssetDetail', () => {
  it('renders the asset when loaded', async () => {
    const fixture = setup('InStock', ['ItAdmin']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('MacBook Pro');
  });

  it('shows lifecycle actions for IT Admin on an in-stock asset', async () => {
    const fixture = setup('InStock', ['ItAdmin']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Send to repair');
  });

  it('hides write actions for the read-only Auditor', async () => {
    const fixture = setup('InStock', ['Auditor']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Send to repair');
  });
});
