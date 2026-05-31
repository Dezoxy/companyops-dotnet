import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
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

import { AuthService } from './core/auth/auth.service';

interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  readonly requiredRoles?: readonly string[];
}

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
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly breakpoints = inject(BreakpointObserver);
  protected readonly auth = inject(AuthService);

  protected readonly nav: readonly NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', path: '/dashboard' },
    { label: 'Requests', icon: 'description', path: '/requests' },
    { label: 'Approvals', icon: 'task_alt', path: '/approvals', requiredRoles: ['Manager', 'Finance'] },
    { label: 'Assets', icon: 'inventory_2', path: '/assets', requiredRoles: ['ItAdmin', 'Auditor'] },
    { label: 'Audit log', icon: 'history', path: '/audit', requiredRoles: ['Auditor'] },
  ];

  protected readonly isHandset = toSignal(
    this.breakpoints.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  // UI-only: hide nav for roles the user lacks (the API still enforces every action).
  protected canSee(item: NavItem): boolean {
    return !item.requiredRoles || item.requiredRoles.some((role) => this.auth.hasRole(role));
  }
}
