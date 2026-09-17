#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
database_name=""

while (($# > 0)); do
  case "$1" in
    --database)
      if (($# < 2)) || [[ -z "$2" ]]; then
        echo "--database requires a value." >&2
        exit 1
      fi
      database_name="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ "$database_name" == *";"* ]]; then
  echo "Database name cannot contain a semicolon." >&2
  exit 1
fi

read_dotenv_value() {
  local target_key="$1"
  local file="$2"
  local line
  local key
  local value

  [[ -f "$file" ]] || return 1

  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"

    [[ -z "$line" || "$line" == \#* || "$line" != *=* ]] && continue

    key="${line%%=*}"
    key="${key#"${key%%[![:space:]]*}"}"
    key="${key%"${key##*[![:space:]]}"}"
    [[ "$key" == "$target_key" ]] || continue

    value="${line#*=}"
    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"
    if [[ ${#value} -ge 2 ]]; then
      if [[ "${value:0:1}" == '"' && "${value: -1}" == '"' ]] ||
        [[ "${value:0:1}" == "'" && "${value: -1}" == "'" ]]; then
        value="${value:1:${#value}-2}"
      fi
    fi

    printf '%s' "$value"
    return 0
  done < "$file"

  return 1
}

connection_string=""
mssql_port="1433"

for environment_file in "$repository_root/.env" "$repository_root/.env.local"; do
  if value="$(read_dotenv_value "ConnectionStrings__Rudoger" "$environment_file")"; then
    connection_string="$value"
  fi
  if value="$(read_dotenv_value "MSSQL_PORT" "$environment_file")"; then
    mssql_port="$value"
  fi
done

if [[ ${ConnectionStrings__Rudoger+x} ]]; then
  connection_string="$ConnectionStrings__Rudoger"
fi
if [[ ${MSSQL_PORT+x} ]]; then
  mssql_port="$MSSQL_PORT"
fi

if [[ -z "$connection_string" ]]; then
  echo "ConnectionStrings__Rudoger is missing. Configure .env.local first." >&2
  exit 1
fi
if [[ "$connection_string" == *REPLACE_WITH* ]]; then
  echo "ConnectionStrings__Rudoger still contains a placeholder." >&2
  exit 1
fi
if [[ ! "$mssql_port" =~ ^[0-9]+$ ]] || ((mssql_port < 1 || mssql_port > 65535)); then
  echo "MSSQL_PORT must be an integer between 1 and 65535." >&2
  exit 1
fi

IFS=';' read -r -a segments <<< "$connection_string"
host_segments=()
server_found=false
database_found=false

for segment in "${segments[@]}"; do
  key="${segment%%=*}"
  key="${key#"${key%%[![:space:]]*}"}"
  key="${key%"${key##*[![:space:]]}"}"
  key="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]')"

  if [[ "$key" == "server" || "$key" == "data source" ]]; then
    host_segments+=("Server=localhost,$mssql_port")
    server_found=true
  elif [[ "$key" == "database" || "$key" == "initial catalog" ]]; then
    if [[ -n "$database_name" ]]; then
      host_segments+=("Database=$database_name")
    else
      host_segments+=("$segment")
    fi
    database_found=true
  else
    host_segments+=("$segment")
  fi
done

if [[ "$server_found" != true ]]; then
  echo "ConnectionStrings__Rudoger does not contain a Server or Data Source entry." >&2
  exit 1
fi
if [[ -n "$database_name" && "$database_found" != true ]]; then
  echo "ConnectionStrings__Rudoger does not contain a Database or Initial Catalog entry." >&2
  exit 1
fi

(
  IFS=';'
  printf '%s\n' "${host_segments[*]}"
)
