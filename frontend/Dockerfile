# syntax=docker/dockerfile:1
# Build the Angular client, then serve the static bundle with nginx behind the Traefik edge
# (ADR 0009/0010). apiBaseUrl is the relative '/api' (same-origin — Traefik strips the prefix and
# routes to the API), so the only domain-specific value is the OIDC authority. That's a known
# placeholder substituted at container start from KEYCLOAK_DOMAIN, so one image deploys to any
# domain (ADR 0012).
FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
# `npm install`, not `npm ci`: the macOS-generated lock omits Linux-only optional deps
# (@emnapi/* from the Linux native build packages), which `npm ci` rejects — matches CI.
RUN npm install --no-audit --no-fund
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.31-alpine AS final
# The application builder emits browser assets under dist/<project>/browser.
COPY --from=build /src/dist/companyops/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
# nginx:alpine runs /docker-entrypoint.d/*.sh before starting — pin the OIDC authority there.
COPY docker-entrypoint.sh /docker-entrypoint.d/40-companyops-authority.sh
RUN chmod +x /docker-entrypoint.d/40-companyops-authority.sh
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
  CMD wget -q --spider http://127.0.0.1/ || exit 1
EXPOSE 80
