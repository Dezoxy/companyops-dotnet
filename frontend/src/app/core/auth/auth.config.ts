import { PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

// OIDC Authorization Code + PKCE against Keycloak (the public `companyops-spa` client).
// PKCE is automatic for the code flow; tokens are attached to apiBaseUrl by the interceptor.
export const authConfig: PassedInitialConfig = {
  config: {
    authority: environment.keycloak.authority,
    redirectUrl: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    clientId: environment.keycloak.clientId,
    scope: 'openid profile email',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    secureRoutes: [environment.apiBaseUrl],
  },
};
