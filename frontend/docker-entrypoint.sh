#!/bin/sh
# Pin the OIDC authority baked into the Angular bundle to this deploy's domain. Keycloak shares
# the app's origin under /auth (single-domain deploy), so only the host in the authority URL is
# deploy-specific; apiBaseUrl is the relative '/api' (no substitution needed). Built with the
# placeholder host 'app.REPLACE_ME.example' (environment.ts) and replaced here from APP_DOMAIN,
# so one image deploys to any domain (ADR 0012). Runs via nginx:alpine's /docker-entrypoint.d/
# before the server starts.
set -eu

: "${APP_DOMAIN:?APP_DOMAIN is required (the public host, e.g. app.example.com)}"

# '|' delimiter — domains never contain it. Idempotent: re-running after substitution is a no-op.
find /usr/share/nginx/html -name '*.js' -exec \
    sed -i "s|app\.REPLACE_ME\.example|${APP_DOMAIN}|g" {} +

echo "companyops: pinned OIDC authority host to ${APP_DOMAIN}"
