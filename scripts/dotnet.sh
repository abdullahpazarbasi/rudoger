#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mkdir -p /tmp/rudoger-dotnet-home /tmp/rudoger-nuget

exec docker run --rm \
  --user "$(id -u):$(id -g)" \
  --env HOME=/tmp/dotnet-home \
  --env DOTNET_CLI_HOME=/tmp/dotnet-home \
  --env NUGET_PACKAGES=/tmp/nuget \
  --volume /tmp/rudoger-dotnet-home:/tmp/dotnet-home \
  --volume /tmp/rudoger-nuget:/tmp/nuget \
  --volume "$repository_root:/workspace" \
  --workdir /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet "$@"
