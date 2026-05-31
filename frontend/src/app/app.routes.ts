import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    title: 'Dashboard · CompanyOps',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'requests',
    canActivate: [authGuard],
    loadChildren: () => import('./features/requests/requests.routes').then((m) => m.REQUESTS_ROUTES),
  },
  // approve/reject = Manager/Finance per docs/security.md; IT Admin *fulfils* (its own console,
  // Phase 17), so it is intentionally not an approvals actor.
  {
    path: 'approvals',
    title: 'Approvals · CompanyOps',
    canActivate: [roleGuard('Manager', 'Finance')],
    loadComponent: () => import('./features/approvals/approvals').then((m) => m.Approvals),
  },
  {
    path: 'audit',
    title: 'Audit log · CompanyOps',
    canActivate: [roleGuard('Auditor')],
    loadComponent: () => import('./features/audit/audit-log').then((m) => m.AuditLog),
  },
  { path: '**', redirectTo: 'dashboard' },
];
