// Production defaults. `apiBaseUrl` is the path Traefik routes to the API; the SPA's deployed
// origin + Keycloak hostname are pinned at deploy time (placeholder until the SPA is served).
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  keycloak: {
    authority: 'https://auth.REPLACE_ME.example/realms/companyops',
    clientId: 'companyops-spa',
  },
};
