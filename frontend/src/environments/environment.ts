// Production defaults. `apiBaseUrl` is the path Traefik routes to the API; Keycloak shares the
// SPA's origin under /auth, so the authority host is pinned at deploy time (placeholder until
// the SPA is served — see frontend/docker-entrypoint.sh).
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  keycloak: {
    authority: 'https://app.REPLACE_ME.example/auth/realms/companyops',
    clientId: 'companyops-spa',
  },
};
