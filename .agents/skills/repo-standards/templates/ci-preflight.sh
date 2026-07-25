#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

find_skill_script() {
    local skill="$1" core="$2"
    local candidates=(
        "$REPO_ROOT/.agents/skills/$skill/scripts/$core.sh"
        "$REPO_ROOT/.agents/plugins/marketplace-source/codex-marketplace/plugins/repo-worker-pack/skills/$skill/scripts/$core.sh"
        "$REPO_ROOT/.agents/plugins/marketplace-source/codex-marketplace/plugins/superpowers-plus/skills/$skill/scripts/$core.sh"
        "$REPO_ROOT/.agents/plugins/marketplace-source/codex-marketplace/plugins/house-skills/skills/$skill/scripts/$core.sh"
    )
    for c in "${candidates[@]}"; do
        if [ -f "$c" ]; then echo "$c"; return; fi
    done
    echo "$skill $core wrapper not found" >&2; exit 1
}

CHECK=""
CHANGED_FROM=""
FULL=""
while [ $# -gt 0 ]; do
    case "$1" in
        --check) CHECK="--check" ;;
        --full) FULL="1" ;;
        --changed-from) shift; CHANGED_FROM="$1" ;;
        *) echo "unknown arg: $1" >&2; exit 1 ;;
    esac
    shift
done

MESH=$(find_skill_script generating-index-mesh generate-index-mesh)
REFRESH=$(find_skill_script refreshing-installed-skills refresh-installed-skills)

MESH_ARGS=()
[ -n "$CHECK" ] && MESH_ARGS+=("--check")
[ -z "$FULL" ] && [ -n "$CHANGED_FROM" ] && MESH_ARGS+=("--changed-from" "$CHANGED_FROM")
"$MESH" "${MESH_ARGS[@]}"

REFRESH_ARGS=()
[ -n "$CHECK" ] && REFRESH_ARGS+=("--check")
"$REFRESH" "${REFRESH_ARGS[@]}"

DOCTRINE="$SCRIPT_DIR/validate_agent_mesh.sh"
if [ -f "$DOCTRINE" ]; then
    DOCTRINE_ARGS=()
    [ -n "$CHECK" ] && DOCTRINE_ARGS+=("--check")
    [ -n "$CHANGED_FROM" ] && DOCTRINE_ARGS+=("--changed-from" "$CHANGED_FROM")
    "$DOCTRINE" "${DOCTRINE_ARGS[@]}"
fi
