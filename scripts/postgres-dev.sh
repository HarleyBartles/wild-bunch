#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(
  git -C "$script_dir" rev-parse --show-toplevel 2>/dev/null || printf '%s\n' "$(cd -- "$script_dir/.." && pwd)"
)"
postgres_version='16.14'
postgres_bin_dir_default="$repo_root/.local/postgresql16/bin"
postgres_dev_root="$repo_root/.local/postgres-dev"
data_dir="$postgres_dev_root/data/wildbunch-dev"
log_dir="$postgres_dev_root/logs"
log_file="$log_dir/wildbunch-dev.log"
port=5434
database_name='wildbunch_dev'
host_name='localhost'
validation_connection_string="Host=$host_name;Port=$port;Database=$database_name;Username=postgres"
command="${1:-setup}"
if [[ $# -gt 0 ]]; then
  shift
fi

usage() {
  cat <<'EOF'
Usage: scripts/postgres-dev.sh [install-tools|ensure|setup|start|stop|reset|status|validate|test]
EOF
}

resolve_tool_dir() {
  if [[ -n "${POSTGRES_BIN_DIR:-}" && -x "$POSTGRES_BIN_DIR/initdb" ]]; then
    printf '%s\n' "$POSTGRES_BIN_DIR"
    return 0
  fi

  if [[ -x "$postgres_bin_dir_default/initdb" ]]; then
    printf '%s\n' "$postgres_bin_dir_default"
    return 0
  fi

  if command -v initdb >/dev/null 2>&1; then
    dirname "$(command -v initdb)"
    return 0
  fi

  return 1
}

resolve_tool() {
  local name="$1"
  local tool_dir

  if tool_dir="$(resolve_tool_dir)"; then
    if [[ -x "$tool_dir/$name" ]]; then
      printf '%s\n' "$tool_dir/$name"
      return 0
    fi
  fi

  if command -v "$name" >/dev/null 2>&1; then
    command -v "$name"
    return 0
  fi

  return 1
}

initdb_cmd="$(resolve_tool initdb || true)"
pg_ctl_cmd="$(resolve_tool pg_ctl || true)"
pg_isready_cmd="$(resolve_tool pg_isready || true)"
createdb_cmd="$(resolve_tool createdb || true)"
dropdb_cmd="$(resolve_tool dropdb || true)"
psql_cmd="$(resolve_tool psql || true)"

require_tooling() {
  if [[ -z "$initdb_cmd" || -z "$pg_ctl_cmd" || -z "$pg_isready_cmd" || -z "$createdb_cmd" || -z "$dropdb_cmd" || -z "$psql_cmd" ]]; then
    echo "PostgreSQL tooling is required for the bash workflow." >&2
    echo "Install PostgreSQL $postgres_version command-line tools and ensure initdb, pg_ctl, pg_isready, createdb, dropdb, and psql are on PATH." >&2
    echo "You can also point POSTGRES_BIN_DIR at a directory containing those binaries." >&2
    exit 1
  fi
}

tool_version() {
  "$initdb_cmd" --version | sed -E 's/^.* ([0-9]+\.[0-9]+(\.[0-9]+)?).*/\1/'
}

write_tooling_instructions() {
  echo "PostgreSQL tooling is expected from native Linux/PostgreSQL binaries."
  echo "Set POSTGRES_BIN_DIR or add the tools to PATH."
}

ensure_directory() {
  local path="$1"
  mkdir -p "$path"
}

set_postgres_setting() {
  local config_path="$1"
  local setting_name="$2"
  local setting_value="$3"
  local tmp_file
  tmp_file="$(mktemp)"

  awk -v name="$setting_name" -v value="$setting_value" '
    BEGIN { found = 0 }
    $0 ~ "^[[:space:]]*#?[[:space:]]*" name "[[:space:]]*=" {
      print name " = " value
      found = 1
      next
    }
    { print }
    END {
      if (!found) {
        print name " = " value
      }
    }
  ' "$config_path" >"$tmp_file"
  mv "$tmp_file" "$config_path"
}

test_cluster_running() {
  [[ -f "$data_dir/PG_VERSION" ]] || return 1
  "$pg_isready_cmd" -h "$host_name" -p "$port" -U postgres >/dev/null 2>&1
}

initialize_cluster() {
  require_tooling

  if [[ ! -f "$data_dir/PG_VERSION" ]]; then
    ensure_directory "$(dirname "$data_dir")"
    "$initdb_cmd" -D "$data_dir" --auth=trust --encoding=UTF8 -U postgres >/dev/null
  fi

  local config_path="$data_dir/postgresql.conf"
  set_postgres_setting "$config_path" port "$port"
  set_postgres_setting "$config_path" listen_addresses "'$host_name'"
}

start_cluster() {
  if test_cluster_running; then
    return 0
  fi

  ensure_directory "$log_dir"
  "$pg_ctl_cmd" -D "$data_dir" -l "$log_file" -w -o "-p $port -h $host_name" start >/dev/null
}

stop_cluster() {
  if test_cluster_running; then
    "$pg_ctl_cmd" -D "$data_dir" -m fast stop >/dev/null
  fi
}

wait_for_ready() {
  local attempt
  for attempt in $(seq 1 30); do
    if "$pg_isready_cmd" -h "$host_name" -p "$port" -U postgres >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done

  echo "PostgreSQL did not become ready on ${host_name}:${port}." >&2
  return 1
}

ensure_database() {
  local query="SELECT 1 FROM pg_database WHERE datname = '$database_name';"
  local result

  result="$("$psql_cmd" -h "$host_name" -p "$port" -U postgres -d postgres -tAc "$query")"
  if [[ "${result//[[:space:]]/}" != '1' ]]; then
    "$createdb_cmd" -h "$host_name" -p "$port" -U postgres "$database_name" >/dev/null
  fi
}

initialize_postgres_validation_lane() {
  initialize_cluster
  start_cluster
  wait_for_ready
  ensure_database
}

run_with_validation_connection_string() {
  env ConnectionStrings__WildBunchPostgresDb="$validation_connection_string" "$@"
}

invoke_validation_lane() {
  initialize_postgres_validation_lane

  run_with_validation_connection_string dotnet tool restore >/dev/null
  run_with_validation_connection_string dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api >/dev/null
  run_with_validation_connection_string dotnet test WildBunch.sln >/dev/null

  echo "PostgreSQL validation lane completed."
  echo "Connection string: $validation_connection_string"
  echo "Direct PostgreSQL-backed dotnet test runs must either use this lane or export ConnectionStrings__WildBunchPostgresDb themselves."
  echo "Use 'scripts/postgres-dev.sh status' to check the shared service. Do not stop it during normal worker cleanup; it is reused by other workers and worktrees."
}

invoke_targeted_test_lane() {
  local test_arguments=("$@")
  local dotnet_arguments=()

  if [[ ${#test_arguments[@]} -gt 0 && "${test_arguments[0]}" == '--' ]]; then
    test_arguments=("${test_arguments[@]:1}")
  fi

  if [[ ${#test_arguments[@]} -eq 0 ]]; then
    echo "Usage: scripts/postgres-dev.sh test -- <dotnet command or dotnet test arguments>" >&2
    return 1
  fi

  if [[ "${test_arguments[0]}" == 'dotnet' ]]; then
    dotnet_arguments=("${test_arguments[@]:1}")
  else
    dotnet_arguments=(test "${test_arguments[@]}")
  fi

  initialize_postgres_validation_lane
  run_with_validation_connection_string dotnet "${dotnet_arguments[@]}"

  echo "PostgreSQL targeted test lane completed."
  echo "Connection string: $validation_connection_string"
  echo "Direct PostgreSQL-backed dotnet runs must either use this lane or export ConnectionStrings__WildBunchPostgresDb themselves."
}

case "$command" in
  install-tools)
    require_tooling
    current_version="$(tool_version)"
    if [[ "$current_version" != "$postgres_version" ]]; then
      write_tooling_instructions
      echo "Expected PostgreSQL tooling version $postgres_version but found $current_version." >&2
      exit 1
    fi
    echo "PostgreSQL tooling is already pinned at version $current_version."
    ;;
  setup)
    initialize_postgres_validation_lane
    echo "Persistent local development database ready."
    echo "Connection string: $validation_connection_string"
    ;;
  start)
    initialize_postgres_validation_lane
    echo "Persistent local development database started."
    ;;
  ensure)
    initialize_postgres_validation_lane
    echo "Shared local PostgreSQL service is ready on ${host_name}:${port}."
    echo "Service owned by persistent checkout: $repo_root"
    echo "Reuse this service from any worktree; do not stop it during normal worker cleanup."
    ;;
  stop)
    require_tooling
    stop_cluster
    echo "Persistent local development database stopped."
    ;;
  reset)
    require_tooling
    stop_cluster
    rm -rf "$data_dir"
    rm -f "$log_file"
    initialize_postgres_validation_lane
    echo "Persistent local development database reset."
    ;;
  status)
    require_tooling
    if [[ ! -f "$data_dir/PG_VERSION" ]]; then
      echo "Cluster not initialized at $data_dir."
      exit 0
    fi

    if test_cluster_running; then
      echo "Cluster is running on ${host_name}:${port}."
      query="SELECT 1 FROM pg_database WHERE datname = '$database_name';"
      result="$("$psql_cmd" -h "$host_name" -p "$port" -U postgres -d postgres -tAc "$query")"
      if [[ "${result//[[:space:]]/}" == '1' ]]; then
        echo "Persistent app database '$database_name' exists."
      else
        echo "Persistent app database '$database_name' is missing."
      fi
    else
      echo "Cluster exists but is not running on ${host_name}:${port}."
      echo "Persistent app database '$database_name' status is unavailable until the cluster is started."
    fi
    ;;
  validate)
    invoke_validation_lane
    ;;
  test)
    invoke_targeted_test_lane "$@"
    ;;
  -h|--help|help)
    usage
    ;;
  *)
    echo "Unknown command: $command" >&2
    usage >&2
    exit 1
    ;;
esac
