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
  // Core workflow screens land in Phase 14; placeholders keep the (guarded) shell navigable.
  {
    path: 'requests',
    title: 'Requests · CompanyOps',
    data: { title: 'Requests' },
    canActivate: [authGuard],
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  // approve/reject = Manager/Finance per docs/security.md; IT Admin *fulfils* (its own console,
  // Phase 17), so it is intentionally not an approvals actor.
  {
    path: 'approvals',
    title: 'Approvals · CompanyOps',
    data: { title: 'Approvals' },
    canActivate: [roleGuard('Manager', 'Finance')],
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  {
    path: 'audit',
    title: 'Audit log · CompanyOps',
    data: { title: 'Audit log' },
    canActivate: [roleGuard('Auditor')],
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  { path: '**', redirectTo: 'dashboard' },
];
