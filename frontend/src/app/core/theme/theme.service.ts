import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'companyops.theme';
const PREFERENCES: readonly ThemePreference[] = ['light', 'dark', 'system'];

/**
 * Persists the user's theme preference (localStorage) and applies it by setting the document
 * root's `color-scheme`, which drives the `light-dark()` design tokens in `styles.scss`. `system`
 * follows the OS (`light dark`). Client-only — a UI preference; the API has no stake in it. Injected
 * by the app shell so the stored preference is applied at startup.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly _preference = signal<ThemePreference>(this.read());

  readonly preference = this._preference.asReadonly();

  constructor() {
    this.apply(this._preference());
  }

  set(preference: ThemePreference): void {
    this._preference.set(preference);
    this.persist(preference);
    this.apply(preference);
  }

  private apply(preference: ThemePreference): void {
    // `light-dark()` resolves from the root element's used color-scheme; 'system' = follow the OS.
    this.document.documentElement.style.colorScheme = preference === 'system' ? 'light dark' : preference;
  }

  private read(): ThemePreference {
    const stored = this.safeGet();
    return stored !== null && (PREFERENCES as readonly string[]).includes(stored) ? (stored as ThemePreference) : 'system';
  }

  private persist(preference: ThemePreference): void {
    try {
      this.document.defaultView?.localStorage.setItem(STORAGE_KEY, preference);
    } catch {
      // localStorage may be unavailable (private mode / disabled); the preference just won't persist.
    }
  }

  private safeGet(): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(STORAGE_KEY) ?? null;
    } catch {
      return null;
    }
  }
}
