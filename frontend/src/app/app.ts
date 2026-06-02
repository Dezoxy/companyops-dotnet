import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuthService } from './core/auth/auth.service';
import { ThemeService, type ThemePreference } from './core/theme/theme.service';
import { environment } from '../environments/environment';

interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  readonly requiredRoles?: readonly string[];
}

// Most-significant role first — drives the single role badge shown in the top bar.
const ROLE_PRIORITY: readonly string[] = ['ItAdmin', 'Finance', 'Manager', 'Auditor', 'Employee'];
const ROLE_LABELS: Readonly<Record<string, string>> = {
  ItAdmin: 'IT Admin',
  Finance: 'Finance',
  Manager: 'Manager',
  Auditor: 'Auditor',
  Employee: 'Employee',
};

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly breakpoints = inject(BreakpointObserver);
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);

  // Non-production environment badge (prod shows none).
  protected readonly envBadge = environment.production ? null : environment.name;

  protected readonly themeOptions: readonly { value: ThemePreference; label: string; icon: string }[] = [
    { value: 'light', label: 'Light', icon: 'light_mode' },
    { value: 'dark', label: 'Dark', icon: 'dark_mode' },
    { value: 'system', label: 'System', icon: 'contrast' },
  ];

  protected readonly themeIcon = computed(
    () => this.themeOptions.find((o) => o.value === this.theme.preference())?.icon ?? 'contrast',
  );

  // The single role to show as a badge (most significant the user holds).
  protected readonly primaryRole = computed(() => {
    const roles = this.auth.roles();
    const role = ROLE_PRIORITY.find((r) => roles.includes(r));
    return role ? (ROLE_LABELS[role] ?? role) : null;
  });

  // Avatar initials from the display name / username ("Tom Horvath" → "TH", "manager.eng" → "ME").
  protected readonly initials = computed(() => {
    const name = this.auth.userName();
    if (!name) {
      return '?';
    }
    const parts = name.split(/[.\s_-]+/).filter(Boolean);
    const letters = parts.length >= 2 ? parts[0][0] + parts[1][0] : name.slice(0, 2);
    return letters.toUpperCase();
  });

  protected readonly nav: readonly NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', path: '/dashboard' },
    { label: 'Requests', icon: 'description', path: '/requests' },
    { label: 'Approvals', icon: 'task_alt', path: '/approvals', requiredRoles: ['Manager', 'Finance'] },
    { label: 'Fulfilment', icon: 'assignment_turned_in', path: '/fulfilment', requiredRoles: ['ItAdmin'] },
    { label: 'Assets', icon: 'inventory_2', path: '/assets', requiredRoles: ['ItAdmin', 'Auditor'] },
    { label: 'Reports', icon: 'insights', path: '/reports', requiredRoles: ['Manager', 'Finance', 'ItAdmin', 'Auditor'] },
    { label: 'Integrations', icon: 'hub', path: '/integrations', requiredRoles: ['ItAdmin', 'Auditor'] },
    { label: 'Audit log', icon: 'history', path: '/audit', requiredRoles: ['Auditor'] },
    { label: 'Settings', icon: 'settings', path: '/settings' },
  ];

  protected readonly isHandset = toSignal(
    this.breakpoints.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  // UI-only: hide nav for roles the user lacks (the API still enforces every action).
  protected canSee(item: NavItem): boolean {
    return !item.requiredRoles || item.requiredRoles.some((role) => this.auth.hasRole(role));
  }

  protected setTheme(preference: ThemePreference): void {
    this.theme.set(preference);
  }
}
