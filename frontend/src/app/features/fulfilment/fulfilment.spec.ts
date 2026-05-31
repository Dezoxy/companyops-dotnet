import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { Fulfilment } from './fulfilment';
import { RequestsService } from '../requests/requests.service';
import { RequestVm } from '../requests/requests.models';

function vm(over: Partial<RequestVm> = {}): RequestVm {
  return {
    id: 'r',
    title: 'A request',
    description: null,
    type: 'Helpdesk',
    typeLabel: 'Helpdesk',
    priority: 'Medium',
    priorityMeta: { label: 'Medium', tone: 'info' },
    category: null,
    categoryLabel: null,
    status: 'Approved',
    statusMeta: { label: 'Approved', tone: 'success' },
    requesterId: 'u',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    fulfilledAssetId: null,
    approvalSteps: [],
    ...over,
  };
}

function setup(requests: RequestVm[]) {
  const service = {
    requests: signal(requests),
    loading: signal(false),
    error: signal(false),
    loadAll: () => undefined,
  } as unknown as RequestsService;

  TestBed.configureTestingModule({
    imports: [Fulfilment],
    providers: [provideRouter([]), provideNoopAnimations(), { provide: RequestsService, useValue: service }],
  });
  return TestBed.createComponent(Fulfilment);
}

describe('Fulfilment', () => {
  it('queues only approved requests (the work awaiting IT)', async () => {
    const fixture = setup([
      vm({ id: 'a', title: 'approved-1', status: 'Approved' }),
      vm({ id: 'b', title: 'submitted-1', status: 'Submitted' }),
      vm({ id: 'c', title: 'completed-1', status: 'Completed' }),
    ]);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('tr[mat-row]').length).toBe(1);
    expect(el.textContent).toContain('approved-1');
    expect(el.textContent).not.toContain('submitted-1');
  });

  it('orders the queue by priority — most urgent first', async () => {
    const fixture = setup([
      vm({ id: 'low', title: 'low-pri', priority: 'Low', priorityMeta: { label: 'Low', tone: 'neutral' } }),
      vm({ id: 'crit', title: 'crit-pri', priority: 'Critical', priorityMeta: { label: 'Critical', tone: 'danger' } }),
    ]);
    await fixture.whenStable();

    const rows = [...(fixture.nativeElement as HTMLElement).querySelectorAll('tr[mat-row]')].map((r) => r.textContent ?? '');
    expect(rows[0]).toContain('crit-pri');
    expect(rows[1]).toContain('low-pri');
  });

  it('shows an empty state when nothing awaits fulfilment', async () => {
    const fixture = setup([vm({ status: 'Submitted' })]);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nothing is awaiting fulfilment.');
  });

  it('summarises the approved queue by type', async () => {
    const fixture = setup([
      vm({ id: '1', type: 'AssetLifecycle' }),
      vm({ id: '2', type: 'AssetLifecycle' }),
      vm({ id: '3', type: 'Helpdesk' }),
    ]);
    await fixture.whenStable();

    const tiles = [...(fixture.nativeElement as HTMLElement).querySelectorAll('.stat')].map((tile) => ({
      label: tile.querySelector('.stat-label')?.textContent?.trim(),
      value: tile.querySelector('.stat-value')?.textContent?.trim(),
    }));
    expect(tiles).toContainEqual({ label: 'Awaiting fulfilment', value: '3' });
    expect(tiles).toContainEqual({ label: 'Asset requests', value: '2' });
    expect(tiles).toContainEqual({ label: 'Helpdesk', value: '1' });
  });
});
