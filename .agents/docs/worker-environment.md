# Worker Environment

Use this reference when working with connectors, handling images, running dev services, or managing worker cleanup.

## Connector / Tool Safety
- Read-only verification must stay read-only.
- Do not call GitHub mutation tools while inspecting repo state, reassessing an issue, or preparing a dispatch.
- Treat tools named `create_*`, `update_*`, `delete_*`, `add_*`, `remove_*`, `lock_*`, `unlock_*`, or low-level Git primitives such as `create_tree` / `create_commit` as mutation routes.
- `create_tree` is not a repo-listing tool. If a tree/listing read route is unavailable, use `fetch_file`, `fetch`, `search`, `compare_commits`, issue readers, and commit/status readers instead.
- Workers do not close GitHub issues; they only return source-backed closeout evidence and recommendations.

## Devin Desktop Environment

### Image Handling
- When users paste images with Ctrl+V, agents can view and analyze them directly as part of the conversation context.
- The `read` tool is for text files only and cannot read binary image files. Do not attempt to use `read` to view pasted images or image files on disk.
- If a user pastes an image, simply describe and analyze what you see in the image. Do not ask the user to describe it or try to read it with the `read` tool.
- Image viewing works through the IDE integration layer, not through file system tools.

## Worker Environment
- The worker environment uses PowerShell, so do not use `&&` for command chaining.
- Run commands separately or use PowerShell-safe sequencing when multiple commands are needed.
- The PostgreSQL service is the shared `Z:\_postgres-cluster` instance on `localhost:5435`. Do not stop it during normal worker cleanup. `.\tools\postgres-dev.ps1 stop` and `reset` change shared state and require explicit lifecycle intent. Run `.\tools\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests.
- Background shell circuit breaker — do not poll a "still running" shell indefinitely. If a backgrounded command that should have completed (setup, ensure, build, test) reports "still running" with no new output for several consecutive `get_output` polls, stop waiting and verify the expected outcome directly. For service-provisioning scripts (`postgres-dev.ps1 ensure`, `dev-servers.ps1 ensure`, etc.), run `status` in a separate foreground shell to check whether the service is already up and healthy. If it is, the background shell is hung (a known Windows handle-inheritance class of bug where a spawned daemon holds the parent pipe open) — kill it and proceed. Never let a single hung shell block an entire session; the expected end state is observable through `status` or other read-only checks, independent of the shell that started the service.
- Dev servers (API and Vite) are **worktree-owned**, not shared like PostgreSQL. Use `.\scripts\dev-servers.ps1 ensure`; see [the UI browser check playbook](ui-browser-check-playbook.md) for port ownership and evidence rules.
- When you start worker-owned API servers, Vite dev servers, test servers, browsers, watch processes, or other long-running helpers, record what you started and clean them up before returning `GREEN` unless you explicitly return `AMBER` or `BLOCKED` with exact process/port evidence.
- When validation touches the workspace, verify cleanup from the workspace perspective before returning `GREEN`: account for likely worker-owned server, browser, watcher, and test-helper processes; include process id, process name, and command line for anything stopped or left running; confirm no repo/file-lock risk remains; report any resources that could not be cleaned up and why.
