import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialogRef } from '@angular/material/dialog';

import { FulfilAssetDialog } from './fulfil-asset-dialog';
import { AssetsService } from '../assets/assets.service';
import { AssetVm } from '../assets/assets.models';

function asset(over: Partial<AssetVm>): AssetVm {
  return {
    id: 'a',
    tag: 'AST-1',
    name: 'Laptop',
    type: 'Laptop',
    typeLabel: 'Laptop',
    status: 'InStock',
    statusMeta: { label: 'In stock', tone: 'info' },
    assignedToId: null,
    createdAt: new Date('2026-05-01T00:00:00Z'),
    ...over,
  };
}

function setup(list: AssetVm[], error = false) {
  const fakeAssets = {
    assets: signal(list).asReadonly(),
    loading: signal(false).asReadonly(),
    error: signal(error).asReadonly(),
    loadAll: () => undefined,
  } as unknown as AssetsService;

  TestBed.configureTestingModule({
    imports: [FulfilAssetDialog],
    providers: [
      provideNoopAnimations(),
      { provide: AssetsService, useValue: fakeAssets },
      { provide: MatDialogRef, useValue: { close: () => undefined } },
    ],
  });
  const fixture = TestBed.createComponent(FulfilAssetDialog);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('FulfilAssetDialog', () => {
  it('shows an empty message and disables confirm when nothing is in stock', () => {
    const el = setup([asset({ status: 'Assigned' }), asset({ status: 'Retired' })]);

    expect(el.textContent).toContain('No in-stock assets');
    const confirm = [...el.querySelectorAll('button')].find((b) => b.textContent?.includes('fulfil'));
    expect(confirm?.disabled).toBe(true);
  });

  it('offers a picker when at least one asset is in stock', () => {
    const el = setup([asset({ id: 'in', tag: 'AST-IN', status: 'InStock' }), asset({ id: 'out', status: 'Retired' })]);

    expect(el.textContent).not.toContain('No in-stock assets');
    expect(el.querySelector('mat-select')).not.toBeNull();
  });

  it('shows a load error (not the empty message) when the asset list fails to load', () => {
    const el = setup([], true);

    expect(el.textContent).toContain("Couldn't load assets");
    expect(el.textContent).not.toContain('No in-stock assets');
  });
});
