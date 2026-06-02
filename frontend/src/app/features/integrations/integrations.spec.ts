import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { Integrations } from './integrations';
import { IntegrationsService } from './integrations.service';
import { IntegrationStatusDto, IntegrationStatusVm, mapIntegrationStatus } from './integrations.models';

const PUBLISHED = {
  id: 'a',
  type: 'RequestApproved',
  status: 'Published' as const,
  occurredAtUtc: '2026-05-01T00:00:00Z',
  processedAtUtc: '2026-05-01T00:00:01Z',
  attempts: 0,
  error: null,
};

describe('mapIntegrationStatus', () => {
  it('maps the summary into tiles and the messages with status tones', () => {
    const dto: IntegrationStatusDto = {
      outbox: { total: 3, pending: 1, published: 1, failed: 1 },
      processedByWorker: 2,
      recent: [
        PUBLISHED,
        {
          id: 'b',
          type: 'RequestFulfilled',
          status: 'Failed',
          occurredAtUtc: '2026-05-01T00:00:00Z',
          processedAtUtc: null,
          attempts: 2,
          error: 'broker down',
        },
      ],
    };

    const vm = mapIntegrationStatus(dto);

    expect(vm.tiles.map((t) => [t.label, t.value])).toEqual([
      ['Total', 3],
      ['Pending', 1],
      ['Published', 1],
      ['Failed', 1],
      ['Processed by worker', 2],
    ]);
    expect(vm.messages[0].statusMeta).toEqual({ label: 'Published', tone: 'success' });
    expect(vm.messages[1].statusMeta).toEqual({ label: 'Failed', tone: 'danger' });
    expect(vm.messages[1].error).toBe('broker down');
  });
});

function fakeService(status: IntegrationStatusVm | null, opts: { loading?: boolean; error?: boolean } = {}): IntegrationsService {
  return {
    status: signal(status),
    loading: signal(opts.loading ?? false),
    error: signal(opts.error ?? false),
    load: () => undefined,
  } as unknown as IntegrationsService;
}

function setup(service: IntegrationsService) {
  TestBed.configureTestingModule({
    imports: [Integrations],
    providers: [provideNoopAnimations(), { provide: IntegrationsService, useValue: service }],
  });
  return TestBed.createComponent(Integrations);
}

describe('Integrations', () => {
  it('renders summary tiles and recent message rows', async () => {
    const vm = mapIntegrationStatus({
      outbox: { total: 1, pending: 0, published: 1, failed: 0 },
      processedByWorker: 1,
      recent: [PUBLISHED],
    });
    const fixture = setup(fakeService(vm));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Published');
    expect(el.textContent).toContain('RequestApproved');
    expect(el.querySelectorAll('.messages-table tbody tr').length).toBe(1);
  });

  it('shows an empty state when there are no messages', async () => {
    const vm = mapIntegrationStatus({
      outbox: { total: 0, pending: 0, published: 0, failed: 0 },
      processedByWorker: 0,
      recent: [],
    });
    const fixture = setup(fakeService(vm));
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No integration messages yet.');
  });

  it('shows the error state instead of the table', async () => {
    const fixture = setup(fakeService(null, { error: true }));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain("Couldn't load integration status.");
    expect(el.querySelector('.messages-table')).toBeNull();
  });
});
