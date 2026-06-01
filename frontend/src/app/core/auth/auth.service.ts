import { Injectable, computed, inject, signal } from '@angular/core';
import { LoginResponse, OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable, tap } from 'rxjs';

interface AuthState {
  readonly isAuthenticated: boolean;
  readonly userId: string | null;
  readonly userName: string | null;
  readonly email: string | null;
  readonly roles: readonly string[];
}

// Keycloak realm roles live in the ACCESS token under realm_access.roles.
interface AccessTokenPayload {
  readonly sub?: string;
  readonly preferred_username?: string;
  readonly email?: string;
  readonly realm_access?: { readonly roles?: string[] };
}

// ID-token / userinfo claims (from the `profile` / `email` scopes) — best source for a display name.
interface UserData {
  readonly sub?: string;
  readonly preferred_username?: string;
  readonly name?: string;
  readonly email?: string;
}

/**
 * Thin wrapper over OidcSecurityService that exposes the session as signals. Components and guards
 * depend on this, not the OIDC library directly. Roles come from the (library-validated) access
 * token's `realm_access.roles`; the display name from the ID-token/userinfo claims. Read-only — the
 * API re-checks every role on every call; the UI only uses these to show/hide.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oidc = inject(OidcSecurityService);
  private readonly state = signal<AuthState>({
    isAuthenticated: false,
    userId: null,
    userName: null,
    email: null,
    roles: [],
  });

  readonly isAuthenticated = computed(() => this.state().isAuthenticated);
  /** The authenticated user's id (`sub`) — matches a request's RequesterId for "own" checks. */
  readonly userId = computed(() => this.state().userId);
  readonly userName = computed(() => this.state().userName);
  readonly email = computed(() => this.state().email);
  readonly roles = computed(() => this.state().roles);

  /** Process the redirect callback / restore the session, then seed state. Awaited at app start. */
  init(): Observable<LoginResponse> {
    // Logout / session loss flips this to false → clear. We deliberately don't re-seed on a
    // `true` emission: the claims are set by the checkAuth() path below, `isAuthenticated$` carries
    // no token to read, and a mid-session role change is rare — it surfaces on the next reload, and
    // the API re-checks every role regardless.
    this.oidc.isAuthenticated$.subscribe((result) => {
      if (!result.isAuthenticated) {
        this.clear();
      }
    });
    // Seed from checkAuth's response — the redirect callback on login, or the restored session on
    // reload. We decode the access token *from the response* instead of re-reading storage at the
    // isAuthenticated$ edge: the library can flip isAuthenticated before getPayloadFromAccessToken()
    // sees the freshly-stored token, which left roles + name empty. The response always carries the
    // token, so this is race-proof.
    return this.oidc.checkAuth().pipe(tap((response) => this.applyFromResponse(response)));
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

  private applyFromResponse(response: LoginResponse): void {
    if (!response.isAuthenticated) {
      this.clear();
      return;
    }
    const access = this.decodeJwt(response.accessToken);
    const user = (response.userData ?? {}) as UserData;
    this.state.set({
      isAuthenticated: true,
      userId: user.sub ?? access?.sub ?? null,
      // Prefer a real display name; fall back to the username (Keycloak's preferred_username).
      userName: user.name || user.preferred_username || access?.preferred_username || null,
      email: user.email ?? access?.email ?? null,
      roles: access?.realm_access?.roles ?? [],
    });
  }

  private clear(): void {
    this.state.set({ isAuthenticated: false, userId: null, userName: null, email: null, roles: [] });
  }

  /** Decode a JWT payload (base64url). No verification — the library already validated the token. */
  private decodeJwt(token: string | null): AccessTokenPayload | null {
    if (!token) {
      return null;
    }
    try {
      const part = token.split('.')[1];
      const b64 = part.replace(/-/g, '+').replace(/_/g, '/');
      const padded = b64.padEnd(b64.length + ((4 - (b64.length % 4)) % 4), '=');
      return JSON.parse(atob(padded)) as AccessTokenPayload;
    } catch (error) {
      // The library already validated the token, so this should never happen — but if it does,
      // make it observable rather than silently dropping the user's roles.
      console.warn('[AuthService] could not decode the access token payload', error);
      return null;
    }
  }
}
