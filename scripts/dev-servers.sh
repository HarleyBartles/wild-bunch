#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"
state_dir="$repo_root/.local/dev-servers"
state_file="$state_dir/state.env"
log_dir="$state_dir/logs"
canonical_api_port=5275
canonical_vite_port=5173
postgres_connection_string='Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres'
health_retry_delays=(2 4 8 16 32)

usage() {
  cat <<'EOF'
Usage: scripts/dev-servers.sh [ensure|start|stop|status]
EOF
}

ensure_directories() {
  mkdir -p "$state_dir" "$log_dir"
}

git_branch() {
  git -C "$repo_root" branch --show-current 2>/dev/null || echo unknown
}

pid_alive() {
  local pid="$1"
  kill -0 "$pid" >/dev/null 2>&1
}

port_in_use() {
  local port="$1"
  python3 - "$port" <<'PY'
import socket
import sys

port = int(sys.argv[1])
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
    sock.settimeout(0.25)
    try:
        sock.connect(("127.0.0.1", port))
    except OSError:
        sys.exit(1)
sys.exit(0)
PY
}

find_free_port() {
  local start_port="$1"
  python3 - "$start_port" <<'PY'
import socket
import sys

start = int(sys.argv[1])
for port in range(start, 65535):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        try:
            sock.bind(("127.0.0.1", port))
        except OSError:
            continue
        print(port)
        raise SystemExit(0)
raise SystemExit(1)
PY
}

port_owned_by_worktree() {
  local port="$1"
  python3 - "$repo_root" "$port" <<'PY'
from pathlib import Path
import os
import sys

repo_root = Path(sys.argv[1]).resolve()
port = int(sys.argv[2])

def iter_listening_inodes():
    target = f"{port:04X}"
    for proc_net in (Path("/proc/net/tcp"), Path("/proc/net/tcp6")):
        try:
            with proc_net.open("r", encoding="utf-8", errors="ignore") as handle:
                next(handle, None)
                for line in handle:
                    parts = line.split()
                    if len(parts) < 10:
                        continue
                    local_address = parts[1]
                    state = parts[3]
                    inode = parts[9]
                    if state != "0A":
                        continue
                    if ":" not in local_address:
                        continue
                    _, local_port = local_address.split(":", 1)
                    if local_port.upper() == target:
                        yield inode
        except FileNotFoundError:
            continue

def pid_matches_repo(pid):
    base = Path("/proc") / pid
    try:
        cwd = (base / "cwd").resolve()
        if repo_root == cwd or repo_root in cwd.parents:
            return True
    except OSError:
        pass

    try:
        cmdline = (base / "cmdline").read_bytes().replace(b"\0", b" ").decode("utf-8", "ignore")
        if str(repo_root) in cmdline:
            return True
    except OSError:
        pass

    return False

inode_to_pid = {}
for pid_dir in Path("/proc").iterdir():
    if not pid_dir.name.isdigit():
        continue
    fd_dir = pid_dir / "fd"
    if not fd_dir.is_dir():
        continue
    try:
        for fd_path in fd_dir.iterdir():
            try:
                target = os.readlink(fd_path)
            except OSError:
                continue
            if target.startswith("socket:[") and target.endswith("]"):
                inode_to_pid.setdefault(target[8:-1], pid_dir.name)
    except OSError:
        continue

for inode in iter_listening_inodes():
    pid = inode_to_pid.get(inode)
    if pid and pid_matches_repo(pid):
        raise SystemExit(0)

raise SystemExit(1)
PY
}

port_listener_pid() {
  local port="$1"
  python3 - "$port" <<'PY'
from pathlib import Path
import os
import sys

port = int(sys.argv[1])

def iter_listening_inodes():
    target = f"{port:04X}"
    for proc_net in (Path("/proc/net/tcp"), Path("/proc/net/tcp6")):
        try:
            with proc_net.open("r", encoding="utf-8", errors="ignore") as handle:
                next(handle, None)
                for line in handle:
                    parts = line.split()
                    if len(parts) < 10:
                        continue
                    local_address = parts[1]
                    state = parts[3]
                    inode = parts[9]
                    if state != "0A":
                        continue
                    if ":" not in local_address:
                        continue
                    _, local_port = local_address.split(":", 1)
                    if local_port.upper() == target:
                        yield inode
        except FileNotFoundError:
            continue

inode_to_pid = {}
for pid_dir in Path("/proc").iterdir():
    if not pid_dir.name.isdigit():
        continue
    fd_dir = pid_dir / "fd"
    if not fd_dir.is_dir():
        continue
    try:
        for fd_path in fd_dir.iterdir():
            try:
                target = os.readlink(fd_path)
            except OSError:
                continue
            if target.startswith("socket:[") and target.endswith("]"):
                inode_to_pid.setdefault(target[8:-1], pid_dir.name)
    except OSError:
        continue

for inode in iter_listening_inodes():
    pid = inode_to_pid.get(inode)
    if pid:
        print(pid)
        raise SystemExit(0)

raise SystemExit(1)
PY
}

http_ok() {
  local url="$1"
  if command -v curl >/dev/null 2>&1; then
    curl -fsS --max-time 5 "$url" >/dev/null
    return
  fi

  python3 - "$url" <<'PY'
from urllib.request import urlopen
import sys

url = sys.argv[1]
try:
    with urlopen(url, timeout=5) as response:
        sys.exit(0 if 200 <= response.status < 300 else 1)
except Exception:
    sys.exit(1)
PY
}

api_healthy() {
  local url="$1"
  http_ok "$url/health"
}

vite_healthy() {
  local url="$1"
  http_ok "$url"
}

wait_for_healthy() {
  local name="$1"
  local url="$2"
  local probe_fn="$3"
  local attempt

  for attempt in "${health_retry_delays[@]}"; do
    if "$probe_fn" "$url"; then
      return 0
    fi
    sleep "$attempt"
  done

  if "$probe_fn" "$url"; then
    return 0
  fi

  echo "$name server did not become healthy on $url after retries." >&2
  return 1
}

write_state() {
  local api_pid="$1"
  local vite_pid="$2"
  local api_port="$3"
  local vite_port="$4"
  local api_url="$5"
  local vite_url="$6"

  ensure_directories
  {
    printf 'checkout_root=%q\n' "$repo_root"
    printf 'worktree_root=%q\n' "$repo_root"
    printf 'branch=%q\n' "$(git_branch)"
    printf 'api_pid=%q\n' "$api_pid"
    printf 'vite_pid=%q\n' "$vite_pid"
    printf 'api_port=%q\n' "$api_port"
    printf 'vite_port=%q\n' "$vite_port"
    printf 'api_url=%q\n' "$api_url"
    printf 'vite_url=%q\n' "$vite_url"
    printf 'started_at=%q\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  } >"$state_file"
}

load_state() {
  if [[ ! -f "$state_file" ]]; then
    return 1
  fi

  # shellcheck disable=SC1090
  source "$state_file"
}

clear_state() {
  rm -f "$state_file"
}

stop_process() {
  local pid="$1"
  if [[ -z "$pid" || "$pid" == 0 ]]; then
    return 0
  fi

  if pid_alive "$pid"; then
    kill "$pid" >/dev/null 2>&1 || true
    wait "$pid" >/dev/null 2>&1 || true
  fi
}

resolve_ports() {
  api_port="$canonical_api_port"
  vite_port="$canonical_vite_port"
  reason='canonical ports free'

  if ! port_in_use "$canonical_api_port" && ! port_in_use "$canonical_vite_port"; then
    return 0
  fi

  if port_owned_by_worktree "$canonical_api_port" || port_owned_by_worktree "$canonical_vite_port"; then
    reason='canonical port(s) already owned by this worktree'
    return 0
  fi

  api_port="$(find_free_port $((canonical_api_port + 1)))"
  vite_port="$(find_free_port $((canonical_vite_port + 1)))"
  reason='canonical ports occupied by another process; allocated fallback ports'
}

start_api_server() {
  local port="$1"
  api_url="http://127.0.0.1:$port"

  dotnet build "$repo_root/src/WildBunch.Api" --nologo >/dev/null
  ensure_directories
  nohup env \
    ConnectionStrings__WildBunchPostgresDb="$postgres_connection_string" \
    ASPNETCORE_ENVIRONMENT='Development' \
    dotnet run --project "$repo_root/src/WildBunch.Api" --urls "$api_url" \
    >"$log_dir/api.log" 2>&1 &
  api_pid=$!

  if wait_for_healthy "API" "$api_url" api_healthy; then
    return 0
  fi

  stop_process "$api_pid"
  return 1
}

start_vite_server() {
  local port="$1"
  local api_url="$2"
  vite_url="http://127.0.0.1:$port"

  pushd "$repo_root/src/WildBunch.Web" >/dev/null
  npm run build >/dev/null
  nohup env \
    VITE_API_BASE_URL="$api_url" \
    npm run dev -- --port "$port" --strictPort \
    >"$log_dir/vite.log" 2>&1 &
  vite_pid=$!
  popd >/dev/null

  if wait_for_healthy "Vite" "$vite_url" vite_healthy; then
    return 0
  fi

  stop_process "$vite_pid"
  return 1
}

invoke_ensure() {
  local api_pid=0
  local vite_pid=0
  local api_port
  local vite_port
  local api_url
  local vite_url
  local reason

  if load_state; then
    if pid_alive "${api_pid:-0}" && pid_alive "${vite_pid:-0}" \
      && api_healthy "${api_url:-}" && vite_healthy "${vite_url:-}"; then
      write_state "$api_pid" "$vite_pid" "${api_port:-0}" "${vite_port:-0}" "$api_url" "$vite_url"
      echo "Dev servers already running for this worktree (reused)."
      echo "  Checkout:  $repo_root"
      echo "  Worktree:  $repo_root"
      echo "  State:     $state_file"
      echo "  Branch:    $(git_branch)"
      echo "  API:       $api_url (PID $api_pid)"
      echo "  Frontend:  $vite_url (PID $vite_pid)"
      echo "  Reused:    yes (state file matched live processes)"
      return 0
    fi

    if pid_alive "${api_pid:-0}" || pid_alive "${vite_pid:-0}"; then
      echo "Stale dev-server state detected; cleaning up and restarting."
      stop_process "${api_pid:-0}"
      stop_process "${vite_pid:-0}"
    fi
    clear_state
  fi

  if port_owned_by_worktree "$canonical_api_port" || port_owned_by_worktree "$canonical_vite_port"; then
    api_port="$canonical_api_port"
    vite_port="$canonical_vite_port"
    api_url="http://127.0.0.1:$api_port"
    vite_url="http://127.0.0.1:$vite_port"
    api_pid="$(port_listener_pid "$api_port" || true)"
    vite_pid="$(port_listener_pid "$vite_port" || true)"
    echo "Canonical dev-server ports are already owned by this worktree."

    if [[ -n "${api_pid:-}" && -n "${vite_pid:-}" ]] \
      && api_healthy "$api_url" && vite_healthy "$vite_url"; then
      write_state "$api_pid" "$vite_pid" "$api_port" "$vite_port" "$api_url" "$vite_url"
      echo "Dev servers already running for this worktree (reused)."
      echo "  Checkout:  $repo_root"
      echo "  Worktree:  $repo_root"
      echo "  State:     $state_file"
      echo "  Branch:    $(git_branch)"
      echo "  API:       $api_url (PID $api_pid)"
      echo "  Frontend:  $vite_url (PID $vite_pid)"
      echo "  Reused:    yes (canonical ports already owned by this worktree)"
      return 0
    fi

    echo "Existing dev servers for this worktree are unhealthy; restarting."
    stop_process "${api_pid:-0}"
    stop_process "${vite_pid:-0}"
    clear_state
  fi

  resolve_ports

  if [[ "${reason:-}" == 'canonical ports free' ]]; then
    :
  fi

  echo "Starting dev servers for this worktree."
  echo "  Checkout:  $repo_root"
  echo "  Worktree:  $repo_root"
  echo "  State:     $state_file"
  echo "  Branch:    $(git_branch)"
  echo "  Reason:    $reason"

  api_pid=0
  api_url=
  if ! start_api_server "$api_port"; then
    return 1
  fi
  echo "  API:       $api_url (PID $api_pid)"

  vite_pid=0
  vite_url=
  if ! start_vite_server "$vite_port" "$api_url"; then
    stop_process "$api_pid"
    clear_state
    return 1
  fi
  echo "  Frontend:  $vite_url (PID $vite_pid)"

  write_state "$api_pid" "$vite_pid" "$api_port" "$vite_port" "$api_url" "$vite_url"

  echo
  echo "Dev servers are ready."
  echo "  Checkout:  $repo_root"
  echo "  API:       $api_url"
  echo "  Frontend:  $vite_url"
  if [[ "$api_port" -ne "$canonical_api_port" || "$vite_port" -ne "$canonical_vite_port" ]]; then
    echo "  NOTE: Non-canonical ports were used because canonical ports were occupied by another worktree."
    echo "  Report these actual URLs in browser-proof returns."
  fi
  echo
  echo "To stop: scripts/dev-servers.sh stop"
  echo "To check: scripts/dev-servers.sh status"
}

invoke_stop() {
  if ! load_state; then
    echo "No dev-server state file found for this worktree."
    echo "Nothing to stop."
    return 0
  fi

  local stopped_api=0
  local stopped_vite=0

  if pid_alive "${api_pid:-0}"; then
    stop_process "$api_pid"
    stopped_api=1
    echo "Stopped API server (PID ${api_pid:-0}) on ${api_url:-unknown}."
  fi

  if pid_alive "${vite_pid:-0}"; then
    stop_process "$vite_pid"
    stopped_vite=1
    echo "Stopped Vite dev server (PID ${vite_pid:-0}) on ${vite_url:-unknown}."
  fi

  if [[ "$stopped_api" -eq 0 && "$stopped_vite" -eq 0 ]]; then
    echo "Dev-server PIDs in state file are no longer alive."
  fi

  clear_state
  echo "Dev-server state cleared for this worktree."
}

invoke_status() {
  local branch
  branch="$(git_branch)"

  echo "Worktree:  $repo_root"
  echo "Checkout:  $repo_root"
  echo "State:     $state_file"
  echo "Branch:    $branch"

  if ! load_state; then
    echo "State:     no state file found"
    echo
    echo "No dev servers recorded for this worktree."
    echo "Run scripts/dev-servers.sh ensure to start them."
    return 0
  fi

  local api_alive=0
  local vite_alive=0
  local api_health=0
  local vite_health=0

  if pid_alive "${api_pid:-0}"; then
    api_alive=1
    if api_healthy "${api_url:-}"; then
      api_health=1
    fi
  fi

  if pid_alive "${vite_pid:-0}"; then
    vite_alive=1
    if vite_healthy "${vite_url:-}"; then
      vite_health=1
    fi
  fi

  echo "State:     recorded at ${started_at:-unknown}"
  if [[ "$api_health" -eq 1 ]]; then
    echo "API:       ${api_url:-unknown} (PID ${api_pid:-0}) - healthy"
  elif [[ "$api_alive" -eq 1 ]]; then
    echo "API:       ${api_url:-unknown} (PID ${api_pid:-0}) - alive but not responding"
  else
    echo "API:       ${api_url:-unknown} (PID ${api_pid:-0}) - dead"
  fi

  if [[ "$vite_health" -eq 1 ]]; then
    echo "Frontend:  ${vite_url:-unknown} (PID ${vite_pid:-0}) - healthy"
  elif [[ "$vite_alive" -eq 1 ]]; then
    echo "Frontend:  ${vite_url:-unknown} (PID ${vite_pid:-0}) - alive but not responding"
  else
    echo "Frontend:  ${vite_url:-unknown} (PID ${vite_pid:-0}) - dead"
  fi

  if [[ "$api_alive" -eq 0 || "$vite_alive" -eq 0 ]]; then
    echo
    echo "Stale state detected. Run scripts/dev-servers.sh ensure to clean up and restart."
  fi
}

command="${1:-ensure}"
if [[ $# -gt 0 ]]; then
  shift
fi

case "$command" in
  ensure|start)
    invoke_ensure
    ;;
  stop)
    invoke_stop
    ;;
  status)
    invoke_status
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
