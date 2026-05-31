#!/bin/sh
# Pin the OIDC authority baked into the Angular bundle to this deploy's Keycloak host. The SPA's
# apiBaseUrl is the relative '/api' (no substitution needed); only the Keycloak authority is
# domain-specific. Built as the placeholder host 'auth.REPLACE_ME.example' (environment.ts) and
# replaced here from KEYCLOAK_DOMAIN, so one image deploys to any domain (ADR 0012). Runs via
# nginx:alpine's /docker-entrypoint.d/ before the server starts.
set -eu

: "${KEYCLOAK_DOMAIN:?KEYCLOAK_DOMAIN is required (the Keycloak host, e.g. auth.example.com)}"

# '|' delimiter — domains never contain it. Idempotent: re-running after substitution is a no-op.
find /usr/share/nginx/html -name '*.js' -exec \
    sed -i "s|auth\.REPLACE_ME\.example|${KEYCLOAK_DOMAIN}|g" {} +

echo "companyops: pinned OIDC authority host to ${KEYCLOAK_DOMAIN}"
