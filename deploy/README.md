# Private deployment learning exercise

This directory contains deployment machinery for the existing Wild Bunch application. It does not add player authentication or make the game available to the public. The AWS environment remains an explicitly authorized, temporary exercise and is not created by local container commands.

The three Linux images are built from the repository root so the frontend can include the production game assets. They target `linux/amd64`, use pinned base-image digests and carry the full source revision in their image tags. The API and migration images use the .NET 10 runtime, while the frontend serves the Vite build through an unprivileged Nginx process on port 8080.

Build images only from a clean committed checkout:

```powershell
py -3 -m tools.deployment.images build --source-sha (git rev-parse HEAD)
```

The frontend uses a same-origin `/api` path. Nginx forwards that path without stripping its prefix, returns 404 for missing asset files, and falls back to `index.html` for browser routes. `X-Wild-Bunch-Release` identifies the source revision without exposing configuration or secrets.

The migration image contains a self-contained EF Core migration bundle. It receives its database connection from a runtime environment variable or Kubernetes Secret, never from an image layer or command-line argument. Production API startup does not apply migrations; the release process runs the bundle as a distinct, controlled step before changing API Pods.

See the plan and specification in `.agents/plans/` and `.agents/specs/` for the full private, disposable deployment design and its authorization gates.
