import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';

import { AuthService } from '../../core/auth/auth.service';
import { ThemePreference, ThemeService } from '../../core/theme/theme.service';
import { StatusChip } from '../../shared/status-chip/status-chip';
import { environment } from '../../../environments/environment';

interface ThemeOption {
  readonly value: ThemePreference;
  readonly label: string;
  readonly icon: string;
}

/**
 * Settings & profile: the signed-in user's identity (read-only — it comes from the OIDC session
 * and is managed in Keycloak) and local UI preferences (theme, persisted on this device). No
 * backend — the API has no stake in UI prefs, and account management lives in Keycloak's own
 * account console (linked out to).
 */
@Component({
  selector: 'app-settings',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatButtonToggleModule, StatusChip],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings {
  private readonly auth = inject(AuthService);
  private readonly theme = inject(ThemeService);

  protected readonly userName = this.auth.userName;
  protected readonly email = this.auth.email;
  protected readonly userId = this.auth.userId;
  protected readonly roles = this.auth.roles;
  protected readonly preference = this.theme.preference;

  // The Keycloak account console (manage password / profile / sessions) — external to this SPA.
  protected readonly accountUrl = `${environment.keycloak.authority}/account`;

  protected readonly themeOptions: readonly ThemeOption[] = [
    { value: 'light', label: 'Light', icon: 'light_mode' },
    { value: 'dark', label: 'Dark', icon: 'dark_mode' },
    { value: 'system', label: 'System', icon: 'contrast' },
  ];

  protected setTheme(preference: ThemePreference): void {
    this.theme.set(preference);
  }
}
