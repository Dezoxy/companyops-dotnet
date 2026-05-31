import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { AuditLog } from './audit-log';
import { AuditService } from './audit.service';

describe('AuditLog', () => {
  it('shows the empty state when there are no entries', async () => {
    const service = {
      logs: signal([]),
      loading: signal(false),
      error: signal(false),
      loadAll: () => undefined,
    } as unknown as AuditService;

    TestBed.configureTestingModule({
      imports: [AuditLog],
      providers: [provideNoopAnimations(), { provide: AuditService, useValue: service }],
    });
    const fixture = TestBed.createComponent(AuditLog);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No audit entries.');
  });
});
