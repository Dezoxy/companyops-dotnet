import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { AssetsList } from './assets-list';
import { AssetsService } from '../assets.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AssetStatus, AssetVm } from '../assets.models';

function vm(name: string, status: AssetStatus = 'InStock'): AssetVm {
  return {
    id: name,
    tag: `AST-${name}`,
    name,
    type: 'Laptop',
    typeLabel: 'Laptop',
    status,
    statusMeta: { label: status, tone: 'info' },
    assignedToId: null,
    createdAt: new Date('2026-05-01T00:00:00Z'),
  };
}

function setup(assets: AssetVm[], roles: string[]) {
  const service = {
    assets: signal(assets),
    loading: signal(false),
    error: signal(false),
    loadAll: () => undefined,
  } as unknown as AssetsService;
  const auth = { hasRole: (r: string) => roles.includes(r) } as unknown as AuthService;

  TestBed.configureTestingModule({
    imports: [AssetsList],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: AssetsService, useValue: service },
      { provide: AuthService, useValue: auth },
    ],
  });
  return TestBed.createComponent(AssetsList);
}

describe('AssetsList', () => {
  it('shows the empty state when there are no assets', async () => {
    const fixture = setup([], ['ItAdmin']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No assets yet');
  });

  it('renders a row per asset', async () => {
    const fixture = setup([vm('alpha'), vm('beta')], ['ItAdmin']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('tr[mat-row]').length).toBe(2);
  });

  it('shows Register for IT Admin', async () => {
    const fixture = setup([], ['ItAdmin']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Register asset');
  });

  it('hides Register for the read-only Auditor', async () => {
    const fixture = setup([], ['Auditor']);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Register asset');
  });
});
