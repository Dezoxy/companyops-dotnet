import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

import { Settings } from './settings';
import { AuthService } from '../../core/auth/auth.service';
import { ThemePreference, ThemeService } from '../../core/theme/theme.service';
import { environment } from '../../../environments/environment';

function fakeAuth(): AuthService {
  return {
    userName: signal<string | null>('jane.doe'),
    email: signal<string | null>('jane@example.com'),
    userId: signal<string | null>('sub-123'),
    roles: signal<readonly string[]>(['Manager', 'Employee']),
  } as unknown as AuthService;
}

function fakeTheme(): { service: ThemeService; calls: ThemePreference[] } {
  const calls: ThemePreference[] = [];
  const service = {
    preference: signal<ThemePreference>('system'),
    set: (preference: ThemePreference) => calls.push(preference),
  } as unknown as ThemeService;
  return { service, calls };
}

function setup(auth: AuthService, theme: ThemeService) {
  TestBed.configureTestingModule({
    imports: [Settings],
    providers: [
      provideNoopAnimations(),
      { provide: AuthService, useValue: auth },
      { provide: ThemeService, useValue: theme },
    ],
  });
  return TestBed.createComponent(Settings);
}

describe('Settings', () => {
  it('renders the profile from the session', async () => {
    const fixture = setup(fakeAuth(), fakeTheme().service);
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('jane.doe');
    expect(text).toContain('jane@example.com');
    expect(text).toContain('Manager');
    expect(text).toContain('sub-123');
  });

  it('links to the Keycloak account console (new tab, no opener)', async () => {
    const fixture = setup(fakeAuth(), fakeTheme().service);
    await fixture.whenStable();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a[target="_blank"]');
    expect(link?.getAttribute('href')).toBe(`${environment.keycloak.authority}/account`);
    expect(link?.getAttribute('rel')).toContain('noopener');
    expect(link?.getAttribute('rel')).toContain('noreferrer');
  });

  it('offers the three theme options', async () => {
    const fixture = setup(fakeAuth(), fakeTheme().service);
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Light');
    expect(text).toContain('Dark');
    expect(text).toContain('System');
  });

  it('sets the theme when a toggle is clicked', async () => {
    const { service, calls } = fakeTheme();
    const fixture = setup(fakeAuth(), service);
    await fixture.whenStable();

    const darkToggle = [...(fixture.nativeElement as HTMLElement).querySelectorAll('mat-button-toggle button')].find((b) =>
      b.textContent?.includes('Dark'),
    ) as HTMLButtonElement | undefined;
    darkToggle?.click();
    await fixture.whenStable();

    expect(calls).toContain('dark');
  });
});
