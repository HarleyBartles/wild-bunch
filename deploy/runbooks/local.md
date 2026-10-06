# Private local Kubernetes exercise

The local exercise uses the existing `kind-wild-bunch-learning` Kubernetes context, the kind `standard` storage class, a private PostgreSQL 16.15 StatefulSet and loopback port forwarding. It does not create an AWS resource or expose a public route. The namespace and its PersistentVolumeClaim contain disposable synthetic game state.

Start from a clean committed checkout. Build all three application images for that commit, then initialize the local database and deploy the application release:

```powershell
py -3 -m tools.deployment.images build --source-sha (git rev-parse HEAD)
py -3 -m tools.deployment.local up
py -3 -m tools.deployment.local release
py -3 -m tools.deployment.local port-forward
```

The `up` command checks the exact Kubernetes context, loads the three commit-tagged application images plus PostgreSQL into kind, creates the exercise namespace and immutable local Secrets, waits for PostgreSQL, and initializes separate migration and runtime credentials. Generated password values stay in process memory and are delivered to Kubernetes through standard input. Reruns reuse the Secrets held in the namespace and never reset established database roles.

The release command applies one uniquely named migration Job and waits for success before updating or scaling either application Deployment. A failed Job is retained for inspection and stops the release. Migration and API workloads use separate credentials. The API serves the same game origin through the frontend's internal `/api` proxy; only the browser is exposed, by the loopback frontend port-forward at `http://127.0.0.1:8088`.

Inspect Kubernetes evidence with the explicit context and namespace:

```powershell
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning get pods,services,pvc,jobs
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning rollout status deployment/api
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning rollout status deployment/frontend
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning get pods -o wide
```

Complete the browser setup, start the game in a town and purchase an available item. Use `GET /api/games/{id}` on the loopback origin to verify persisted cash and inventory. Record the API Pod UID locally, delete that API Pod, wait for its replacement, verify the UID changes, and make another game mutation. The same browser origin keeps the saved game identifier stable.

To observe a database outage, record the API Pod restart count, scale PostgreSQL to zero, then start a separate API-only loopback port-forward:

```powershell
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning scale statefulset/postgres --replicas=0
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning port-forward --address 127.0.0.1 service/api 18080:8080
```

While the forward runs, `/health` must return 200 and `/health/ready` must return 503. Confirm the API Pod's restart count did not increase. Restore PostgreSQL and wait for database readiness to return before resuming the saved game:

```powershell
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning scale statefulset/postgres --replicas=1
kubectl --context kind-wild-bunch-learning -n wild-bunch-learning rollout status statefulset/postgres --timeout=180s
```

Developer controls remain denied by the Production API. The existing browser shell may still display developer controls; that UI limitation is outside the deployment slice and is not evidence of backend access.

Review actual Pod requests, limits, restarts and node scheduling during the journey before making capacity claims. Requests are starting values for one frontend, one API, a temporary migration Job and local PostgreSQL; they do not establish high availability or production sizing. `kubectl top` requires a metrics provider and is not implied by this setup.

To discard the namespace, its game data, Jobs, credentials and PVC, verify this is the intended learning environment and use the explicit destructive flag:

```powershell
py -3 -m tools.deployment.local down --confirm-discard
```

The command checks the exact kind context and exercise ownership labels before deleting the namespace. It leaves the kind cluster and cached Docker images available for another local run.
