#!/bin/bash
# Runs once, on first initialization of an empty Postgres data volume (prod stack only).
# Creates the database + user that Keycloak connects with. KC_DB_PASSWORD is passed via the
# postgres container's environment; psql's :'var' quoting handles any special characters
# (the SQL heredoc is single-quoted so the shell never touches the password).
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=kc_pw="$KC_DB_PASSWORD" <<-'EOSQL'
  CREATE USER keycloak WITH PASSWORD :'kc_pw';
  CREATE DATABASE keycloak OWNER keycloak;
EOSQL
