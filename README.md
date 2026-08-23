# Wild Bunch

A C#/.NET Western adventure game with a React/Vite web play surface, Onion/DDD/CQRS/event-sourcing backend, and PostgreSQL persistence.

## For developers

- `AGENTS.md` — auto-injected agent law and routing to repo doctrine. Start here if you are an agent or a contributor working alongside one.
- [INDEX.md](INDEX.md) — generated navigation index for the whole repo.
- [docs/](docs/INDEX.md) — repo documentation and ADR log.

## Run the pre-alpha locally

These steps assume a Windows PowerShell environment. Bash equivalents are in `docs/local-postgresql.md` and `scripts/README.md`.

### 1. Prerequisites

- .NET 10 SDK
- Node.js (npm)
- Git
- PostgreSQL 16.14 command-line tools placed in `.local/postgresql16` (see step 3 for the first-run path)

### 2. Clone the repo

```powershell
git clone https://github.com/HarleyBartles/wild-bunch.git
cd wild-bunch
```

### 3. Start the local PostgreSQL

```powershell
.\scripts\postgres-dev.ps1 ensure
```

This creates or reuses the persistent dev database on `localhost:5434` and the `wildbunch_dev` app database. It is idempotent: re-run it safely any time you are unsure.

**First-run failure and status check:** on a fresh checkout the command can fail with `Missing PostgreSQL binary: .\.local\postgresql16\bin\initdb.exe`. That means the PostgreSQL tooling root is missing. Download PostgreSQL 16.14 for Windows, extract it to `.\.local\postgresql16`, then re-run `ensure`. After that, you can check the cluster state with `.\scripts\postgres-dev.ps1 status`.

### 4. Install dependencies

```powershell
dotnet tool restore
npm --prefix src\WildBunch.Web ci
```

### 5. Launch the API

```powershell
dotnet run --project src\WildBunch.Api
```

Wait for the `Now listening on:` output. The API serves on `http://localhost:5275`; confirm it with `http://localhost:5275/health`.

### 6. Launch the frontend

In a second terminal from the repo root:

```powershell
cd src\WildBunch.Web
npm run dev
```

The Vite dev server serves on `http://localhost:5173`. Open that URL to play.

### 7. Stop

Use `Ctrl+C` in each terminal. The PostgreSQL service is shared and safe to leave running for the next session; when you do want it down, run `.\scripts\postgres-dev.ps1 stop`.

### Shortcuts and validation

If you prefer one command to start both API and frontend, run `.\scripts\dev-servers.ps1 ensure`. For the full build/test/EF validation lane and the PostgreSQL-backed CI path, see `.agents/docs/validation-policy.md`, `docs/local-postgresql.md`, and `scripts/README.md`.

## License

The source code in this repository is licensed under the MIT License; see [LICENSE](LICENSE).

All creative material, including artwork, audio, narrative, characters, worldbuilding, and branding, is copyright Harley Bartles and all rights are reserved. See [LICENSE-ASSETS.md](LICENSE-ASSETS.md) for the inspected paths and the exact boundary between code and creative content.

Wild Bunch is an unofficial, independent re-imagining inspired by the game published by Firebird Software. It was developed from childhood memory without using Firebird's source code, assets, text, screens, or playthroughs. Firebird Software is credited as the original publisher. This project does not claim that Firebird Software is the current rights holder, nor does it claim rights over historical facts or third-party material. Existing third-party licenses in the repository remain in force.

## Repo-local plugin posture

This repo default-installs Codex plugins from [HarleyBartles/agent-asset-marketplace](https://github.com/HarleyBartles/agent-asset-marketplace). Their canonical configuration is [`.agents/plugins/marketplace.json`](.agents/plugins/marketplace.json); vendored skills and provenance are generated from it.
