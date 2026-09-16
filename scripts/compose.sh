#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
environment_file="$repository_root/.env.local"

if [[ ! -f "$environment_file" ]]; then
  echo "Missing .env.local. Copy .env.dist to .env.local and replace the placeholders." >&2
  exit 1
fi

exec docker compose \
  --project-directory "$repository_root" \
  --env-file "$repository_root/.env" \
  --env-file "$environment_file" \
  "$@"
