"""Operate the private local Kubernetes learning environment."""

from __future__ import annotations

import argparse
import base64
import json
import os
import secrets
import socket
import subprocess
import sys
import time
from contextlib import contextmanager
from pathlib import Path
from typing import Iterator

from deploy.database.init import DatabaseEndpoint, PostgresAdmin, initialize_database
from tools.deployment.contracts import (
    ContractError,
    EnvironmentContract,
    ReleaseContract,
    validate_contracts,
)
from tools.deployment.process import CommandFailed, REPOSITORY_ROOT, run_checked
from tools.deployment.release import (
    KubectlReleaseAdapter,
    LocalDirectoryReleaseStore,
    LocalSecretSource,
    ReleaseController,
)


CLUSTER_NAME = "wild-bunch-learning"
KUBERNETES_CONTEXT = f"kind-{CLUSTER_NAME}"
NAMESPACE = "wild-bunch-learning"
REGION = "eu-north-1"
LOCAL_POSTGRES_FORWARD_PORT = 15434
LOCAL_OVERLAY = REPOSITORY_ROOT / "deploy/kubernetes/overlays/local"
NAMESPACE_MANIFEST = REPOSITORY_ROOT / "deploy/kubernetes/base/namespace.yaml"
SECRET_LABELS = {
    "app.kubernetes.io/part-of": NAMESPACE,
    "app.kubernetes.io/managed-by": "wild-bunch-deployment",
}
_IMAGE_NAMES = ("frontend", "api", "migrations")


def _source_sha() -> str:
    source_sha = run_checked(["git", "rev-parse", "HEAD"]).strip()
    if run_checked(["git", "status", "--porcelain"]):
        raise ContractError("Local release commands require a clean committed checkout.")
    return source_sha


def _image_repositories() -> dict[str, str]:
    return {name: f"wild-bunch/{name}" for name in _IMAGE_NAMES}


def _validate_local_context(source_sha: str, actual_context: str) -> None:
    release_id = f"local-{source_sha}-{time.monotonic_ns()}"
    environment = EnvironmentContract(
        region=REGION,
        owner_id="local",
        cluster_name=CLUSTER_NAME,
        namespace=NAMESPACE,
        kubernetes_context=actual_context,
        image_repositories=_image_repositories(),
    )
    release = ReleaseContract(
        owner_id="local",
        release_id=release_id,
        source_sha=source_sha,
        images={name: f"wild-bunch/{name}:{source_sha}" for name in _IMAGE_NAMES},
        config_map_name=f"api-config-{release_id}",
        runtime_secret_name=f"runtime-{release_id}",
        migration_secret_name=f"migration-{release_id}",
        recovery_release_id=None,
        compatibility_acknowledgement=(
            "schema: existing; constraints: unchanged; data/events: compatible; writes: compatible; recovery: none"
        ),
    )
    validate_contracts(
        environment,
        release,
        expected_context=KUBERNETES_CONTEXT,
        expected_region=REGION,
        expected_owner_id="local",
        expected_namespace=NAMESPACE,
        allow_local_tags=True,
    )


def _kubectl(args: list[str], *, stdin: str | None = None) -> str:
    return run_checked(["kubectl", "--context", KUBERNETES_CONTEXT, *args], stdin=stdin)


def _current_context() -> str:
    return run_checked(["kubectl", "config", "current-context"]).strip()


def _ensure_local_images(source_sha: str) -> list[str]:
    tags = [f"wild-bunch/{name}:{source_sha}" for name in _IMAGE_NAMES]
    for image in tags:
        try:
            run_checked(["docker", "image", "inspect", image])
        except CommandFailed as error:
            if error.returncode == 1:
                raise RuntimeError(
                    "A local image is missing; build all three images with tools.deployment.images first."
                ) from None
            raise
    run_checked(["kind", "load", "docker-image", "--name", CLUSTER_NAME, *tags])
    return tags


def _secret_payload(secret_name: str, values: dict[str, str]) -> dict[str, object]:
    data = {
        key: base64.b64encode(value.encode("utf-8")).decode("ascii")
        for key, value in values.items()
    }
    return {
        "apiVersion": "v1",
        "kind": "Secret",
        "metadata": {
            "name": secret_name,
            "namespace": NAMESPACE,
            "labels": SECRET_LABELS,
        },
        "immutable": True,
        "type": "Opaque",
        "data": data,
    }


def _read_secret_data(secret_name: str) -> dict[str, str] | None:
    output = _kubectl(
        ["-n", NAMESPACE, "get", "secret", secret_name, "--ignore-not-found", "-o", "json"]
    )
    if not output.strip():
        return None
    try:
        secret = json.loads(output)
        data = secret["data"]
        return {
            key: base64.b64decode(value, validate=True).decode("utf-8")
            for key, value in data.items()
        }
    except (KeyError, TypeError, ValueError, json.JSONDecodeError):
        raise RuntimeError(f"Kubernetes Secret {secret_name} is malformed; reconcile it before retrying.") from None


def _apply_secret(secret_name: str, values: dict[str, str]) -> None:
    manifest = json.dumps(_secret_payload(secret_name, values), separators=(",", ":"))
    _kubectl(["-n", NAMESPACE, "apply", "-f", "-"], stdin=manifest)


class KubernetesSecretStore:
    """Keep local generated credentials in immutable Kubernetes Secrets only."""

    _secret_names = {
        "runtime": "runtime-database-credential",
        "migration": "migration-database-credential",
    }

    def read(self, name: str) -> dict[str, object] | None:
        secret_data = _read_secret_data(self._secret_names[name])
        if secret_data is None:
            return None
        return {
            "username": secret_data["username"],
            "password": secret_data["password"],
            "host": secret_data["host"],
            "port": int(secret_data["port"]),
            "database": secret_data["database"],
        }

    def create_if_absent(self, name: str, value: dict[str, object]) -> dict[str, object]:
        established = self.read(name)
        if established is not None:
            return established
        connection_string = (
            f"Host={value['host']};Port={value['port']};Database={value['database']};"
            f"Username={value['username']};Password={value['password']};SSL Mode=Disable"
        )
        payload = {key: str(item) for key, item in value.items()}
        payload["connectionString"] = connection_string
        _apply_secret(self._secret_names[name], payload)
        return value


@contextmanager
def _postgres_port_forward() -> Iterator[None]:
    if not _port_available(LOCAL_POSTGRES_FORWARD_PORT):
        raise RuntimeError(
            f"Local PostgreSQL forward port {LOCAL_POSTGRES_FORWARD_PORT} is occupied; free it and retry."
        )
    command = [
        "kubectl",
        "--context",
        KUBERNETES_CONTEXT,
        "-n",
        NAMESPACE,
        "port-forward",
        "--address",
        "127.0.0.1",
        "service/postgres",
        f"{LOCAL_POSTGRES_FORWARD_PORT}:5432",
    ]
    try:
        process = subprocess.Popen(
            command,
            cwd=REPOSITORY_ROOT,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except OSError:
        raise RuntimeError("Could not start the private local PostgreSQL port-forward.") from None
    try:
        for _ in range(100):
            if process.poll() is not None:
                raise RuntimeError("The private local PostgreSQL port-forward exited before it was ready.")
            if not _port_available(LOCAL_POSTGRES_FORWARD_PORT):
                break
            time.sleep(0.1)
        else:
            raise RuntimeError("The private local PostgreSQL port-forward did not become ready.")
        yield
    finally:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


def _port_available(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
        return listener.connect_ex(("127.0.0.1", port)) != 0


def up() -> None:
    source_sha = _source_sha()
    actual_context = _current_context()
    _validate_local_context(source_sha, actual_context)
    _kubectl(["get", "storageclass", "standard", "-o", "name"])
    _ensure_local_images(source_sha)

    _kubectl(["apply", "-f", str(NAMESPACE_MANIFEST)])
    admin_secret = _read_secret_data("postgres-admin")
    if admin_secret is None:
        admin_secret = {
            "username": "wb_admin",
            "password": secrets.token_urlsafe(48),
        }
        _apply_secret("postgres-admin", admin_secret)
    if not admin_secret.get("username") or not admin_secret.get("password"):
        raise RuntimeError(
            "The postgres-admin Secret is missing its username or password; reconcile it before retrying."
        )

    rendered = _kubectl(["kustomize", str(LOCAL_OVERLAY)])
    _kubectl(["apply", "-f", "-"], stdin=rendered)
    _kubectl(["-n", NAMESPACE, "rollout", "status", "statefulset/postgres", "--timeout=180s"])

    advertised_endpoint = DatabaseEndpoint(host="postgres", port=5432, sslmode="disable")
    admin_endpoint = DatabaseEndpoint(
        host="127.0.0.1",
        port=LOCAL_POSTGRES_FORWARD_PORT,
        sslmode="disable",
    )
    secret_store = KubernetesSecretStore()
    with _postgres_port_forward():
        initialize_database(
            PostgresAdmin(admin_endpoint, admin_secret["username"], admin_secret["password"]),
            secret_store,
            advertised_endpoint,
        )
    print("Local PostgreSQL and immutable database credentials are ready in the private kind namespace.")
    print("Run `py -3 -m tools.deployment.local release` to apply a migration and start the application.")


def _local_environment(actual_context: str) -> EnvironmentContract:
    repositories = _image_repositories()
    return EnvironmentContract(
        region=REGION,
        owner_id="local",
        cluster_name=CLUSTER_NAME,
        namespace=NAMESPACE,
        kubernetes_context=actual_context,
        image_repositories=repositories,
    )


def _local_release(source_sha: str, release_id: str, recovery_release_id: str | None) -> ReleaseContract:
    repositories = _image_repositories()
    recovery_text = recovery_release_id if recovery_release_id is not None else "none"
    return ReleaseContract(
        owner_id="local",
        release_id=release_id,
        source_sha=source_sha,
        images={name: f"{repositories[name]}:{source_sha}" for name in _IMAGE_NAMES},
        config_map_name=f"api-config-{release_id}",
        runtime_secret_name=f"runtime-{release_id}",
        migration_secret_name=f"migration-{release_id}",
        recovery_release_id=recovery_release_id,
        compatibility_acknowledgement=(
            f"schema: current; constraints: unchanged; data/events: compatible; writes: compatible; recovery: {recovery_text}"
        ),
    )


def _local_store() -> LocalDirectoryReleaseStore:
    local_app_data = os.environ.get("LOCALAPPDATA")
    if not local_app_data:
        raise RuntimeError("LOCALAPPDATA is required for private local release recovery storage.")
    return LocalDirectoryReleaseStore(Path(local_app_data) / "WildBunch" / "releases")


def _release_controller(environment: EnvironmentContract, api_readiness_path: str = "/health/ready") -> ReleaseController:
    kubernetes = KubectlReleaseAdapter(
        environment.kubernetes_context,
        environment.namespace,
        local=True,
        api_readiness_path=api_readiness_path,
    )
    return ReleaseController(
        kubernetes,
        LocalSecretSource(environment.kubernetes_context, environment.namespace),
        _local_store(),
        render_workload=kubernetes.render_manifest,
        expected_context=KUBERNETES_CONTEXT,
        expected_owner_id="local",
        allow_local_tags=True,
    )


def release(api_readiness_path: str = "/health/ready") -> None:
    source_sha = _source_sha()
    actual_context = _current_context()
    _validate_local_context(source_sha, actual_context)
    _ensure_local_images(source_sha)
    environment = _local_environment(actual_context)
    store = _local_store()
    release_id = f"local-{source_sha}-{time.monotonic_ns()}"
    release_contract = _local_release(source_sha, release_id, store.current_release_id())
    record = _release_controller(environment, api_readiness_path).deploy(environment, release_contract)
    print(f"Release {release_contract.release_id} is ready in namespace {NAMESPACE}.")
    print("Open it with `py -3 -m tools.deployment.local port-forward` at http://127.0.0.1:8088.")


def recover(release_id: str) -> None:
    actual_context = _current_context()
    _validate_local_context("0" * 40, actual_context)
    environment = _local_environment(actual_context)
    record = _release_controller(environment).recover(environment, release_id)
    print(f"Recovered known-good release {record.release.release_id} in namespace {NAMESPACE}.")


def port_forward() -> None:
    actual_context = _current_context()
    _validate_local_context("0" * 40, actual_context)
    run_checked(
        [
            "kubectl",
            "--context",
            KUBERNETES_CONTEXT,
            "-n",
            NAMESPACE,
            "port-forward",
            "--address",
            "127.0.0.1",
            "service/frontend",
            "8088:8080",
        ]
    )


def down(confirm_discard: bool) -> None:
    actual_context = _current_context()
    _validate_local_context("0" * 40, actual_context)
    if not confirm_discard:
        raise ContractError(
            "Run local down with --confirm-discard to remove exercise workloads and synthetic game data."
        )
    output = _kubectl(["get", "namespace", NAMESPACE, "-o", "json"])
    try:
        labels = json.loads(output)["metadata"]["labels"]
    except (KeyError, json.JSONDecodeError):
        raise RuntimeError("The namespace is missing expected ownership labels; refusing cleanup.") from None
    if labels.get("app.kubernetes.io/part-of") != NAMESPACE or labels.get(
        "app.kubernetes.io/managed-by"
    ) != "wild-bunch-deployment":
        raise ContractError("Namespace ownership does not match the local exercise; refusing cleanup.")
    _kubectl(["delete", "namespace", NAMESPACE, "--wait=true", "--timeout=180s"])
    print("Local exercise resources and synthetic game data were deleted from the learning namespace.")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("up", help="start PostgreSQL and initialize private local credentials")
    release_parser = commands.add_parser("release", help="run a unique database migration Job before the app rollout")
    release_parser.add_argument(
        "--api-readiness-path",
        choices=("/health/ready", "/health/incorrect"),
        default="/health/ready",
        help="Use /health/incorrect only to exercise local failed-release recovery.",
    )
    recover_parser = commands.add_parser("recover", help="restore a recorded known-good local release")
    recover_parser.add_argument("--release-id", required=True)
    commands.add_parser("port-forward", help="forward the frontend to loopback port 8088")
    down_parser = commands.add_parser("down", help="delete exercise workloads and synthetic game data")
    down_parser.add_argument("--confirm-discard", action="store_true")
    args = parser.parse_args(argv)

    try:
        if args.command == "up":
            up()
        elif args.command == "release":
            release(args.api_readiness_path)
        elif args.command == "recover":
            recover(args.release_id)
        elif args.command == "port-forward":
            port_forward()
        else:
            down(args.confirm_discard)
    except Exception as error:
        detail = (
            str(error)
            if isinstance(error, (CommandFailed, ContractError, RuntimeError, ValueError))
            else type(error).__name__
        )
        print(f"Local deployment command failed: {detail}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        return 130
    return 0


if __name__ == "__main__":
    sys.exit(main())
