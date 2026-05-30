import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    title: 'Dashboard · CompanyOps',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  // Core workflow screens land in the next Phase 12 chunk; placeholders keep the shell navigable.
  {
    path: 'requests',
    title: 'Requests · CompanyOps',
    data: { title: 'Requests' },
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  {
    path: 'approvals',
    title: 'Approvals · CompanyOps',
    data: { title: 'Approvals' },
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  {
    path: 'audit',
    title: 'Audit log · CompanyOps',
    data: { title: 'Audit log' },
    loadComponent: () => import('./shared/placeholder/placeholder').then((m) => m.Placeholder),
  },
  { path: '**', redirectTo: 'dashboard' },
];
