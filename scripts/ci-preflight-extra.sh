#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"

check_mode=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --check)
      check_mode=1
      ;;
    --changed-from)
      shift
      # Intentionally unused: preflight-extra runs the full repo-specific suite.
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
  shift
done

cd "$repo_root"

echo '--- Repo-local skill validation ---'
python3 "$script_dir/validate_local_skills_extra.py" --check "$repo_root/.agents/skills" wild-bunch-

if [[ $check_mode -eq 1 ]]; then
  echo '--- Repo-specific preflight: --check mode, skipping heavy build/test ---'
  exit 0
fi

echo '--- Python script tests ---'
python3 -m pip install -r "$script_dir/requirements.txt"
python3 -m pytest "$script_dir/tests" -q

echo '--- Backend preflight ---'
dotnet restore WildBunch.sln
dotnet build WildBunch.sln --no-restore --configuration Release
dotnet tool restore
bash "$script_dir/postgres-dev.sh" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
bash "$script_dir/postgres-dev.sh" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release

echo '--- Frontend preflight ---'
pushd src/WildBunch.Web >/dev/null
npm ci
npm run typecheck
npm run test
npm run build
popd >/dev/null
