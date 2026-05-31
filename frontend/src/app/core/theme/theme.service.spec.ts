import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

// The service goes through the injected DOCUMENT token; in jsdom that is the global `document`
// (and its `defaultView.localStorage` is the global `localStorage`), so reading the globals back
// here observes exactly what the service wrote.
describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.style.colorScheme = '';
    TestBed.configureTestingModule({});
  });

  it('applies an explicit theme to the document color-scheme and persists it', () => {
    const theme = TestBed.inject(ThemeService);

    theme.set('dark');

    expect(document.documentElement.style.colorScheme).toBe('dark');
    expect(localStorage.getItem('companyops.theme')).toBe('dark');
  });

  it("maps 'system' to a 'light dark' color-scheme (follow the OS)", () => {
    const theme = TestBed.inject(ThemeService);

    theme.set('system');

    expect(document.documentElement.style.colorScheme).toBe('light dark');
  });

  it('reads and applies the stored preference at startup', () => {
    localStorage.setItem('companyops.theme', 'dark');

    const theme = TestBed.inject(ThemeService);

    expect(theme.preference()).toBe('dark');
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });

  it('defaults to system when nothing (or something invalid) is stored', () => {
    localStorage.setItem('companyops.theme', 'rainbow');

    const theme = TestBed.inject(ThemeService);

    expect(theme.preference()).toBe('system');
  });
});
