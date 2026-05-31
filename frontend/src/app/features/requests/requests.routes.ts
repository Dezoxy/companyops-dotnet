import { Routes } from '@angular/router';
import { RequestsList } from './requests-list/requests-list';
import { RequestDetail } from './request-detail/request-detail';

/** Lazy-loaded as a unit from app.routes.ts under /requests (behind authGuard). */
export const REQUESTS_ROUTES: Routes = [
  { path: '', title: 'Requests · CompanyOps', component: RequestsList },
  { path: ':id', title: 'Request · CompanyOps', component: RequestDetail },
];
