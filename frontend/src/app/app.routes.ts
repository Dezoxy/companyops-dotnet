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
  // IT-Admin fulfilment console: the work queue of approved requests awaiting fulfillment.
  // Fulfilling is IT-Admin-only (FulfillRequests) and the API re-checks, so this gate is UX.
  {
    path: 'fulfilment',
    title: 'Fulfilment · CompanyOps',
    canActivate: [roleGuard('ItAdmin')],
    loadComponent: () => import('./features/fulfilment/fulfilment').then((m) => m.Fulfilment),
  },
  {
    path: 'audit',
    title: 'Audit log · CompanyOps',
    canActivate: [roleGuard('Auditor')],
    loadComponent: () => import('./features/audit/audit-log').then((m) => m.AuditLog),
  },
  // Reports & Analytics: aggregate read-only views for the oversight roles. The API enforces
  // ReadReports (Manager/Finance/IT Admin/Auditor); this gate is UX.
  {
    path: 'reports',
    title: 'Reports · CompanyOps',
    canActivate: [roleGuard('Manager', 'Finance', 'ItAdmin', 'Auditor')],
    loadComponent: () => import('./features/reports/reports').then((m) => m.Reports),
  },
  // Asset console: reads admit IT Admin + the read-only Auditor (docs/security.md); writes
  // are IT-Admin-only and the API re-checks, so the UI hides write actions for the Auditor.
  {
    path: 'assets',
    canActivate: [roleGuard('ItAdmin', 'Auditor')],
    loadChildren: () => import('./features/assets/assets.routes').then((m) => m.ASSETS_ROUTES),
  },
  { path: '**', redirectTo: 'dashboard' },
];
