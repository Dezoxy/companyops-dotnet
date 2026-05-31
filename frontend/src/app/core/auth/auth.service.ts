import { Injectable, computed, inject, signal } from '@angular/core';
import { LoginResponse, OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable } from 'rxjs';

interface AuthState {
  readonly isAuthenticated: boolean;
  readonly userName: string | null;
  readonly roles: readonly string[];
}

interface AccessTokenPayload {
  readonly preferred_username?: string;
  readonly realm_access?: { readonly roles?: string[] };
}

/**
 * Thin wrapper over OidcSecurityService that exposes the session as signals. Components and
 * guards depend on this, not the OIDC library directly. The signal is driven by the library's
 * `isAuthenticated$` stream, so it stays live through silent renew, expiry, and logout. Roles
 * come from the (library-validated) access token's `realm_access.roles` — read-only; the API
 * re-checks every role on every call.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly state = signal<AuthState>({ isAuthenticated: false, userName: null, roles: [] });

  readonly isAuthenticated = computed(() => this.state().isAuthenticated);
  readonly userName = computed(() => this.state().userName);
  readonly roles = computed(() => this.state().roles);

  /** Track the live session and process any redirect callback. Awaited at app start. */
  init(): Observable<LoginResponse> {
    this.oidc.isAuthenticated$.subscribe((result) => this.sync(result.isAuthenticated));
    return this.oidc.checkAuth();
  }

  login(): void {
    this.oidc.authorize();
  }

  logout(): void {
    // Fall back to clearing the local session if the server end-session call fails, so the UI
    // never stays "logged in" after a logout attempt.
    this.oidc.logoff().subscribe({ error: () => this.oidc.logoffLocal() });
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  private sync(isAuthenticated: boolean): void {
    const payload = isAuthenticated
      ? (this.oidc.getPayloadFromAccessToken() as AccessTokenPayload | null)
      : null;
    this.state.set({
      isAuthenticated,
      userName: payload?.preferred_username ?? null,
      roles: payload?.realm_access?.roles ?? [],
    });
  }
}
