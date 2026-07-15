#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"

skip_backend=0
skip_frontend=0
skip_index_mesh=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-backend)
      skip_backend=1
      ;;
    --skip-frontend)
      skip_frontend=1
      ;;
    --skip-index-mesh)
      skip_index_mesh=1
      ;;
    -h|--help)
      cat <<'EOF'
Usage: scripts/ci-preflight.sh [--skip-backend] [--skip-frontend] [--skip-index-mesh]
EOF
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
  shift
done

cd "$repo_root"

if [[ $skip_backend -eq 0 ]]; then
  echo '--- Backend preflight ---'
  dotnet restore WildBunch.sln
  dotnet build WildBunch.sln --no-restore --configuration Release
  dotnet tool restore
  bash "$script_dir/postgres-dev.sh" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
  bash "$script_dir/postgres-dev.sh" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release
fi

if [[ $skip_frontend -eq 0 ]]; then
  echo '--- Frontend preflight ---'
  pushd src/WildBunch.Web >/dev/null
  npm ci
  npm run typecheck
  npm run test
  npm run build
  popd >/dev/null
fi

if [[ $skip_index_mesh -eq 0 ]]; then
  echo '--- Index mesh preflight ---'
  bash "$script_dir/generate_index_mesh.sh" --check

  echo '--- Validating marketplace.json ---'
  python3 -c "import json; m=json.load(open('.agents/plugins/marketplace.json')); assert m['name']=='wild-bunch'; assert len(m['plugins'])==7; assert all(p['policy']['installation']=='INSTALLED_BY_DEFAULT' for p in m['plugins']); assert {p['name'] for p in m['plugins']}=={'repo-worker-pack','superpowers-plus','wild-bunch-project-pack','game-studio','dotnet-kit','architecture-pack','frontend-pack'}; print('OK marketplace.json')"
fi
