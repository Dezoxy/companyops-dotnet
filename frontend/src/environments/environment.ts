// Production defaults. `apiBaseUrl` is the path Traefik routes to the API; Keycloak shares the
// SPA's origin under /auth, so the authority host is pinned at deploy time (placeholder until
// the SPA is served — see frontend/docker-entrypoint.sh).
export const environment = {
  production: true,
  // Defines the `name` field on the environment type (Angular infers the shape from this file).
  // The shell only renders the badge when `production` is false, so prod shows none.
  name: 'Production',
  apiBaseUrl: '/api',
  keycloak: {
    authority: 'https://app.REPLACE_ME.example/auth/realms/companyops',
    clientId: 'companyops-spa',
  },
};
