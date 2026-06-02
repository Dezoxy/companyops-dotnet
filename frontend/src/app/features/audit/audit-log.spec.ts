import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { AuditLog } from './audit-log';
import { AuditService } from './audit.service';
import { AuditLogVm } from './audit.models';

function vm(id: string): AuditLogVm {
  return {
    id,
    occurredAt: new Date('2026-05-01T10:00:00Z'),
    actorLabel: 'user-1',
    action: 'RequestApproved',
    actionMeta: { label: 'Approved', tone: 'success' },
    targetType: 'Request',
    targetId: 'req-00000001',
    targetIdShort: 'REQ-0000',
    fromStatus: 'Submitted',
    toStatus: 'Approved',
  };
}

function setup(entries: AuditLogVm[], total = entries.length) {
  const service = {
    fetchPageResult: () => of({ items: entries, total, page: 1, pageSize: 50 }),
  } as unknown as AuditService;
  TestBed.configureTestingModule({
    imports: [AuditLog],
    providers: [provideNoopAnimations(), { provide: AuditService, useValue: service }],
  });
  return TestBed.createComponent(AuditLog);
}

describe('AuditLog', () => {
  it('shows the empty state when there are no entries', async () => {
    const fixture = setup([], 0);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No audit entries.');
  });

  it('renders a row per entry and the true total in the footer', async () => {
    const fixture = setup([vm('a'), vm('b')], 12408);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('.audit-table tbody tr').length).toBe(2);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('of 12408 entries');
  });
});
