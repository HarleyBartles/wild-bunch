# Validation Policy

Use this reference when running validation, debugging CI failures, or deciding test coverage scope.

## Validation Commands
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet tool restore` before EF validation commands when the repo-local tool manifest is used.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or validation to reuse the shared local service (idempotent: no-op when already healthy).
- Run `.\scripts\postgres-dev.ps1 validate` for the repo-local PostgreSQL-backed validation lane; it provisions the persistent cluster, exports the repo-local connection string for child `dotnet` commands, restores tools, and runs the EF and test checks together.
- For targeted PostgreSQL-backed tests, use `.\scripts\postgres-dev.ps1 test -- <dotnet test args>` so the script sets `ConnectionStrings__WildBunchPostgresDb` in the same process before invoking `dotnet test`; do not rely on a standalone `$env:` assignment in a separate command.
- Use `.\scripts\postgres-dev.ps1 status` to check whether the lane is already running, `setup` or `validate` to provision it, and `reset` for the destructive local app-database reset path. `stop` and `reset` are manual/destructive; do not stop the shared service during normal worker cleanup.
- If PostgreSQL port `5434` is closed or connection setup fails, report the exact command and output after running the repo-local setup/status lane instead of treating it as a product regression.
- Report warnings separately from failures.

## Index Mesh CI Failures

The "Index mesh + plugin manifest" CI job runs `python scripts/generate_index_mesh.py --check` on a clean Linux checkout. It fails when the committed INDEX.md files don't match what the generator produces from the CI tree. Common causes and fixes:

- **Stale INDEX.md after file rename/add/delete:** Regenerate with `python scripts/generate_index_mesh.py` and commit the updated INDEX.md files. The generator walks the live tree, so any renamed/added/deleted file or directory needs an index refresh.
- **`TestResults` directory (gitignored test output):** `TestResults/` is a gitignored directory created by `dotnet test` runs. It contains dynamic GUID-named subdirectories. The generator must exclude it (it is in `EXCLUDED_DIR_NAMES` in `scripts/generate_index_mesh.py`). If a new gitignored output directory appears, add it to `EXCLUDED_DIR_NAMES` and `EXCLUDED_ROOT_NAMES` in the generator script, then regenerate. Do NOT commit INDEX.md files inside gitignored output directories.
- **PowerShell pipe encoding corrupts `git cat-file` output:** When debugging blob contents on Windows, do NOT pipe `git cat-file -p` through PowerShell `|` or `>` — PowerShell converts stdout to UTF-16LE, adding a `\xff\xfe` BOM and wide characters that look like file corruption. Use `git cat-file -p <sha> | python -c "import sys; ..."` with `sys.stdin.buffer.read()` to inspect raw bytes, or write to a file with `git cat-file -p <sha> -o <file>`.
- **`core.autocrlf=true` on Windows:** The repo uses `autocrlf=true` on Windows. Git stores INDEX.md blobs as LF (the generator writes with `newline="\n"`), and autocrlf normalizes on checkout. This is fine — the generator's `normalize_text` strips CRLF before comparing. The CI check is not a line-ending issue; it is a content/tree-structure mismatch.

## Testing Posture
- New or updated real application behavior should normally include test coverage in the same slice.
- If coverage is skipped, state the reason explicitly and keep the gap narrow and deliberate.
- Debug-only or temporary prototype surfaces may use lighter-weight coverage while they remain debug-only.
