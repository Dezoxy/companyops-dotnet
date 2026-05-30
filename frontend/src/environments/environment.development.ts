// Dev (ng serve). `/api` is proxied to the backend (proxy.conf.json). The Keycloak authority
// must match the token issuer the browser sees — run dev Keycloak with KC_HOSTNAME=
// http://localhost:8080 for SPA login (see frontend/CLAUDE.md).
export const environment = {
  production: false,
  apiBaseUrl: '/api',
  keycloak: {
    authority: 'http://localhost:8080/realms/companyops',
    clientId: 'companyops-spa',
  },
};
