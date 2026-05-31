import { Routes } from '@angular/router';

/** Lazy-loaded as a unit from app.routes.ts under /requests (behind authGuard). Each screen is
 *  its own chunk within the requests bundle. `new` is declared before `:id` so it isn't
 *  captured as a request id. */
export const REQUESTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Requests · CompanyOps',
    loadComponent: () => import('./requests-list/requests-list').then((m) => m.RequestsList),
  },
  {
    path: 'new',
    title: 'New request · CompanyOps',
    loadComponent: () => import('./request-create/request-create').then((m) => m.RequestCreate),
  },
  {
    path: ':id',
    title: 'Request · CompanyOps',
    loadComponent: () => import('./request-detail/request-detail').then((m) => m.RequestDetail),
  },
];
