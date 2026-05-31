import { Routes } from '@angular/router';

/** Lazy-loaded as a unit from app.routes.ts under /assets (behind roleGuard ItAdmin/Auditor).
 *  Each screen is its own chunk within the assets bundle. */
export const ASSETS_ROUTES: Routes = [
  {
    path: '',
    title: 'Assets · CompanyOps',
    loadComponent: () => import('./assets-list/assets-list').then((m) => m.AssetsList),
  },
  {
    path: ':id',
    title: 'Asset · CompanyOps',
    loadComponent: () => import('./asset-detail/asset-detail').then((m) => m.AssetDetail),
  },
];
