#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

find_skill_script() {
  local skill="$1" core="$2"
  local installed="$REPO_ROOT/.agents/skills/$skill/scripts/$core.sh"
  if [ -f "$installed" ]; then echo "$installed"; return; fi

  local mp_source="$REPO_ROOT/.agents/plugins/marketplace-source/codex-marketplace/plugins"
  if [ -d "$mp_source" ]; then
    local found
    found=$(find "$mp_source" -path "*/skills/$skill/scripts/$core.sh" -maxdepth 4 -print -quit 2>/dev/null)
    if [ -n "$found" ]; then echo "$found"; return; fi
  fi
  echo "$skill $core wrapper not found" >&2; exit 1
}

CHECK=""
FULL=""
CHANGED_FROM=""
while [ $# -gt 0 ]; do
  case "$1" in
    --check) CHECK="--check" ;;
    --full) FULL="1" ;;
    --changed-from)
      shift
      CHANGED_FROM="$1"
      ;;
    *) echo "unknown arg: $1" >&2; exit 1 ;;
  esac
  shift
done

STANDARDS=$(find_skill_script repo-standards repo-standards)
SCAFFOLD=$(find_skill_script repo-standards scaffold-all)
MESH=$(find_skill_script generating-agent-mesh generate-index-mesh)
VALIDATE=$(find_skill_script generating-agent-mesh validate-agent-mesh)
REFRESH=$(find_skill_script refreshing-installed-skills refresh-installed-skills)

STANDARDS_ARGS=()
[ -n "$CHECK" ] && STANDARDS_ARGS+=("--check")
"$STANDARDS" "${STANDARDS_ARGS[@]}"

SCAFFOLD_ARGS=()
[ -n "$CHECK" ] && SCAFFOLD_ARGS+=("--check")
"$SCAFFOLD" "${SCAFFOLD_ARGS[@]}"

MESH_ARGS=()
[ -n "$CHECK" ] && MESH_ARGS+=("--check")
# generate-index-mesh reconciles the whole tracked mesh; scoped diff is
# handled by validate-agent-mesh and the optional ci-preflight-extra hook.
"$MESH" "${MESH_ARGS[@]}"

VALIDATE_ARGS=()
[ -n "$CHECK" ] && VALIDATE_ARGS+=("--check")
[ -n "$CHANGED_FROM" ] && VALIDATE_ARGS+=("--changed-from" "$CHANGED_FROM")
"$VALIDATE" "${VALIDATE_ARGS[@]}"

REFRESH_ARGS=()
[ -n "$CHECK" ] && REFRESH_ARGS+=("--check")
[ -z "$CHECK" ] && REFRESH_ARGS+=("--allow-shared-checkout")
"$REFRESH" "${REFRESH_ARGS[@]}"

EXTRA="$SCRIPT_DIR/ci-preflight-extra.sh"
if [ -f "$EXTRA" ]; then
  EXTRA_ARGS=()
  [ -n "$CHECK" ] && EXTRA_ARGS+=("--check")
  [ -n "$CHANGED_FROM" ] && EXTRA_ARGS+=("--changed-from" "$CHANGED_FROM")
  "$EXTRA" "${EXTRA_ARGS[@]}"
fi
