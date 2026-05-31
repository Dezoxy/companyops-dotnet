import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Require an authenticated session; otherwise start the Keycloak login. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }
  auth.login();
  return false;
};

/**
 * Require one of `allowed` roles (after authentication). The API still enforces every action —
 * this only keeps the UI honest about what to show.
 */
export function roleGuard(...allowed: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) {
      auth.login();
      return false;
    }
    return allowed.some((role) => auth.hasRole(role)) ? true : router.createUrlTree(['/dashboard']);
  };
}
