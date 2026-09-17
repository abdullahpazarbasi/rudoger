#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_directory/.." && pwd)"
output_path=""
force=false

print_usage() {
  printf '%s\n' \
    "Usage: ./scripts/dump-database.sh [--output PATH] [--force]" \
    "" \
    "Exports the configured Rudoger database schema and table data to a T-SQL file." \
    "The default output is artifacts/database/rudoger-<UTC timestamp>.sql."
}

while (($# > 0)); do
  case "$1" in
    -o | --output)
      if (($# < 2)) || [[ -z "$2" ]]; then
        echo "$1 requires a path." >&2
        exit 1
      fi
      output_path="$2"
      shift 2
      ;;
    -f | --force)
      force=true
      shift
      ;;
    -h | --help)
      print_usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      print_usage >&2
      exit 1
      ;;
  esac
done

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet was not found. Install the .NET 10 SDK." >&2
  exit 1
fi

if [[ -z "$output_path" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  output_path="$repository_root/artifacts/database/rudoger-$timestamp.sql"
fi

extension="${output_path##*.}"
if [[ "$(printf '%s' "$extension" | tr '[:upper:]' '[:lower:]')" != "sql" ]]; then
  echo "Output path must use the .sql extension." >&2
  exit 1
fi

output_directory="$(dirname "$output_path")"
mkdir -p "$output_directory"
output_directory="$(cd "$output_directory" && pwd -P)"
output_path="$output_directory/$(basename "$output_path")"

if [[ -e "$output_path" && "$force" != true ]]; then
  echo "Output already exists: $output_path. Use --force to replace it." >&2
  exit 1
fi

connection_string="$("$script_directory/db-connection-string.sh")"
tool_arguments=(
  run
  --project "$repository_root/Tools/DatabaseDump/Rudoger.DatabaseDump.csproj"
  --configuration Release
  --no-launch-profile
  --
  --output "$output_path"
)
if [[ "$force" == true ]]; then
  tool_arguments+=(--force)
fi

RUDOGER_DATABASE_DUMP_CONNECTION_STRING="$connection_string" dotnet "${tool_arguments[@]}"
unset connection_string
