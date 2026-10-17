"""Ordered database migrations, application release and known-good recovery."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
from dataclasses import asdict, dataclass
from enum import StrEnum
from pathlib import Path
from typing import Callable, Protocol

import boto3
import psycopg
import yaml

from tools.deployment.contracts import EnvironmentContract, ReleaseContract, validate_contracts
from tools.deployment.process import CommandFailed, REPOSITORY_ROOT, run_checked


REGION = "eu-north-1"
NAMESPACE = "wild-bunch-learning"


class ReleaseError(RuntimeError):
    """A release stopped at a known or uncertain boundary; details are safely summarized."""


class JobState(StrEnum):
    ABSENT = "absent"
    RUNNING = "running"
    COMPLETE = "complete"
    FAILED = "failed"


@dataclass(frozen=True)
class SecretValue:
    value: dict[str, object]
    version_id: str


@dataclass(frozen=True)
class ReleaseRecord:
    release: ReleaseContract
    manifest: str
    secret_version_ids: dict[str, str]
    status: str

    def to_json(self) -> str:
        return json.dumps(
            {
                "release": asdict(self.release),
                "manifest": self.manifest,
                "secret_version_ids": self.secret_version_ids,
                "status": self.status,
            },
            sort_keys=True,
            separators=(",", ":"),
        )

    @classmethod
    def from_json(cls, value: str) -> ReleaseRecord:
        try:
            data = json.loads(value)
            release = ReleaseContract(**data["release"])
            record = cls(
                release=release,
                manifest=data["manifest"],
                secret_version_ids=data["secret_version_ids"],
                status=data["status"],
            )
        except (KeyError, TypeError, json.JSONDecodeError):
            raise ReleaseError("Stored release record is invalid; refusing to apply it.") from None
        if not isinstance(record.manifest, str) or not isinstance(record.secret_version_ids, dict):
            raise ReleaseError("Stored release record is invalid; refusing to apply it.")
        return record


class SecretSource(Protocol):
    def get(self, name: str, version_id: str | None = None) -> SecretValue: ...


class ReleaseStore(Protocol):
    def get(self, release_id: str) -> ReleaseRecord | None: ...

    def save(self, record: ReleaseRecord) -> None: ...

    def current_release_id(self) -> str | None: ...

    def set_current_release_id(self, release_id: str) -> None: ...


class ReleaseKubernetes(Protocol):
    def prepare_release_objects(
        self,
        environment: EnvironmentContract,
        release: ReleaseContract,
        runtime_connection: str,
        migration_connection: str,
    ) -> None: ...

    def job_state(self, name: str) -> JobState: ...

    def create_migration_job(
        self, name: str, image: str, secret_name: str, release_id: str, source_sha: str
    ) -> None: ...

    def wait_for_migration_job(self, name: str, timeout_seconds: int) -> bool: ...

    def verify_migration_history(self, runtime_secret: SecretValue, environment: EnvironmentContract) -> bool: ...

    def apply_workload(self, manifest: str) -> None: ...

    def verify_workload(self, release: ReleaseContract, manifest: str) -> None: ...


def _connection_string(secret: SecretValue, environment: EnvironmentContract) -> str:
    values = secret.value
    required = ("username", "password", "host", "port", "database")
    if any(not isinstance(values.get(name), str | int) or not str(values[name]) for name in required):
        raise ReleaseError("Database credential has an incomplete shape.")
    host = str(values["host"])
    if environment.database_host and host != environment.database_host:
        raise ReleaseError("Database credential targets a different endpoint.")
    try:
        port = int(values["port"])
    except (TypeError, ValueError):
        raise ReleaseError("Database credential has an invalid port.") from None
    if not 1 <= port <= 65535:
        raise ReleaseError("Database credential has an invalid port.")

    def quote(value: object) -> str:
        return "'" + str(value).replace("'", "''") + "'"

    root_certificate = ";Root Certificate=/app/certs/rds-ca-bundle.pem" if environment.owner_id != "local" else ""
    ssl_mode = "VerifyFull" if environment.owner_id != "local" else "Disable"
    return (
        f"Host={quote(host)};Port={port};Database={quote(values['database'])};"
        f"Username={quote(values['username'])};Password={quote(values['password'])};"
        f"SSL Mode={ssl_mode}{root_certificate}"
    )


def _migration_job_name(release_id: str) -> str:
    return f"migration-{hashlib.sha256(release_id.encode('utf-8')).hexdigest()[:52]}"


class ReleaseController:
    def __init__(
        self,
        kubernetes: ReleaseKubernetes,
        secrets: SecretSource,
        store: ReleaseStore,
        *,
        render_workload: Callable[[EnvironmentContract, ReleaseContract], str],
        expected_context: str,
        expected_owner_id: str,
        allow_local_tags: bool,
    ) -> None:
        self._kubernetes = kubernetes
        self._secrets = secrets
        self._store = store
        self._render_workload = render_workload
        self._expected_context = expected_context
        self._expected_owner_id = expected_owner_id
        self._allow_local_tags = allow_local_tags

    def _validate(
        self,
        environment: EnvironmentContract,
        release: ReleaseContract,
        *,
        pointer_update_pending: bool = False,
    ) -> None:
        validate_contracts(
            environment,
            release,
            expected_context=self._expected_context,
            expected_region=REGION,
            expected_owner_id=self._expected_owner_id,
            expected_namespace=NAMESPACE,
            allow_local_tags=self._allow_local_tags,
        )
        current = self._store.current_release_id()
        if release.recovery_release_id != current and not (pointer_update_pending and current == release.release_id):
            raise ReleaseError("Selected recovery release does not match the recorded known-good release.")

    def _secret_values(
        self, environment: EnvironmentContract, record: ReleaseRecord | None
    ) -> tuple[dict[str, SecretValue], dict[str, str]]:
        versions = record.secret_version_ids if record is not None else {}
        values: dict[str, SecretValue] = {}
        try:
            for name in ("runtime", "migration"):
                secret = self._secrets.get(name, versions.get(name))
                if not secret.version_id or (versions.get(name) and secret.version_id != versions[name]):
                    raise ValueError("Secret version mismatch")
                values[name] = secret
        except Exception:
            raise ReleaseError("Application credentials could not be retrieved; no credential values were recorded.") from None
        return values, {name: secret.version_id for name, secret in values.items()}

    def deploy(self, environment: EnvironmentContract, release: ReleaseContract) -> ReleaseRecord:
        previous = self._store.get(release.release_id)
        self._validate(
            environment,
            release,
            pointer_update_pending=previous is not None and previous.status == "verified",
        )
        if previous is not None and previous.status in {"failed", "known-good"}:
            raise ReleaseError("This release ID is already terminal; use a distinct authorized attempt.")
        if previous is not None and previous.release.to_json() != release.to_json():
            raise ReleaseError("The release ID is already bound to a different release contract.")
        if previous is not None and previous.status == "verified":
            self._store.set_current_release_id(release.release_id)
            record = ReleaseRecord(release, previous.manifest, previous.secret_version_ids, "known-good")
            self._store.save(record)
            return record

        secrets, versions = self._secret_values(environment, previous)
        runtime_connection = _connection_string(secrets["runtime"], environment)
        migration_connection = _connection_string(secrets["migration"], environment)
        manifest = previous.manifest if previous is not None else self._render_workload(environment, release)
        record = ReleaseRecord(release, manifest, versions, "prepared")
        self._store.save(record)

        try:
            self._kubernetes.prepare_release_objects(
                environment, release, runtime_connection, migration_connection
            )
        except Exception:
            raise ReleaseError("Release configuration could not be prepared; no application rollout occurred.") from None

        job_name = _migration_job_name(release.release_id)
        try:
            state = self._kubernetes.job_state(job_name)
            if state is JobState.FAILED:
                record = ReleaseRecord(release, manifest, versions, "migration-failed")
                self._store.save(record)
                raise ReleaseError(f"Migration Job {job_name} failed; inspect it and use a distinct attempt after diagnosis.")
            if state is JobState.ABSENT:
                self._kubernetes.create_migration_job(
                    job_name,
                    release.images["migrations"],
                    release.migration_secret_name,
                    release.release_id,
                    release.source_sha,
                )
                state = JobState.RUNNING
            if state is JobState.RUNNING and not self._kubernetes.wait_for_migration_job(job_name, 305):
                record = ReleaseRecord(release, manifest, versions, "migration-uncertain")
                self._store.save(record)
                raise ReleaseError(f"Migration Job {job_name} is uncertain; inspect this existing Job before retrying.")
            if self._kubernetes.job_state(job_name) is JobState.FAILED:
                record = ReleaseRecord(release, manifest, versions, "migration-failed")
                self._store.save(record)
                raise ReleaseError(f"Migration Job {job_name} failed; inspect it and use a distinct attempt after diagnosis.")
            if self._kubernetes.job_state(job_name) is not JobState.COMPLETE:
                record = ReleaseRecord(release, manifest, versions, "migration-uncertain")
                self._store.save(record)
                raise ReleaseError(f"Migration Job {job_name} has not completed; inspect this existing Job before retrying.")
        except ReleaseError:
            raise
        except Exception:
            record = ReleaseRecord(release, manifest, versions, "migration-uncertain")
            self._store.save(record)
            raise ReleaseError(f"Migration Job {job_name} status is uncertain; inspect this existing Job before retrying.") from None

        record = ReleaseRecord(release, manifest, versions, "migrated")
        self._store.save(record)
        try:
            if not self._kubernetes.verify_migration_history(secrets["runtime"], environment):
                raise ReleaseError("Migration Job completed but runtime credentials could not verify migration history.")
            self._kubernetes.apply_workload(manifest)
            self._kubernetes.verify_workload(release, manifest)
        except ReleaseError as error:
            if "runtime credentials" in str(error):
                raise
            record = ReleaseRecord(release, manifest, versions, "failed")
            self._store.save(record)
            raise
        except Exception:
            record = ReleaseRecord(release, manifest, versions, "failed")
            self._store.save(record)
            raise ReleaseError("Release operation failed; the previous known-good release remains selected.") from None

        record = ReleaseRecord(release, manifest, versions, "verified")
        self._store.save(record)
        try:
            self._store.set_current_release_id(release.release_id)
        except Exception:
            raise ReleaseError("Release verification passed but the known-good pointer update is uncertain; retry this release ID.") from None
        record = ReleaseRecord(release, manifest, versions, "known-good")
        self._store.save(record)
        return record

    def recover(self, environment: EnvironmentContract, release_id: str) -> ReleaseRecord:
        record = self._store.get(release_id)
        if record is None or record.status != "known-good":
            raise ReleaseError("Recovery target is not a recorded known-good release.")
        release = record.release
        validate_contracts(
            environment,
            release,
            expected_context=self._expected_context,
            expected_region=REGION,
            expected_owner_id=self._expected_owner_id,
            expected_namespace=NAMESPACE,
            allow_local_tags=self._allow_local_tags,
        )
        secrets, _ = self._secret_values(environment, record)
        try:
            self._kubernetes.prepare_release_objects(
                environment,
                release,
                _connection_string(secrets["runtime"], environment),
                _connection_string(secrets["migration"], environment),
            )
            self._kubernetes.apply_workload(record.manifest)
            self._kubernetes.verify_workload(release, record.manifest)
        except Exception:
            raise ReleaseError("Recovery failed; the current release pointer was not changed.") from None
        self._store.set_current_release_id(release_id)
        return record


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    deploy_parser = commands.add_parser("deploy", help="apply one validated release after its migration Job")
    deploy_parser.add_argument("--environment", required=True)
    deploy_parser.add_argument("--release", required=True)
    recover_parser = commands.add_parser("recover", help="restore a recorded known-good application release")
    recover_parser.add_argument("--environment", required=True)
    recover_parser.add_argument("--release-id", required=True)
    inspect_parser = commands.add_parser("inspect", help="inspect one non-secret release record")
    inspect_parser.add_argument("--environment", required=True)
    inspect_parser.add_argument("--release-id", required=True)
    args = parser.parse_args(argv)

    try:
        environment = EnvironmentContract.from_json(Path(args.environment).read_text(encoding="utf-8"))
        store = (
            S3ReleaseStore(environment.release_bucket, environment.release_prefix or "releases", environment.region)
            if environment.release_bucket
            else _local_store()
        )
        if args.command == "inspect":
            record = store.get(args.release_id)
            if record is None:
                raise ReleaseError("Release record was not found.")
            print(json.dumps({
                "release_id": record.release.release_id,
                "source_sha": record.release.source_sha,
                "status": record.status,
                "secret_version_ids": record.secret_version_ids,
                "manifest": record.manifest,
            }, sort_keys=True, indent=2))
            return 0

        if environment.region != REGION or environment.namespace != NAMESPACE:
            raise ReleaseError("The selected release environment is outside the approved region or namespace.")
        actual_context = run_checked(["kubectl", "config", "current-context"]).strip()
        if actual_context != environment.kubernetes_context:
            raise ReleaseError("Current Kubernetes context does not match the selected environment contract.")
        local = environment.owner_id == "local"
        kubernetes = KubectlReleaseAdapter(environment.kubernetes_context, environment.namespace, local=local)
        secrets: SecretSource = LocalSecretSource(environment.kubernetes_context, environment.namespace) if local else AwsSecretSource(environment)
        controller = ReleaseController(
            kubernetes,
            secrets,
            store,
            render_workload=kubernetes.render_manifest,
            expected_context=environment.kubernetes_context,
            expected_owner_id="local" if local else NAMESPACE,
            allow_local_tags=local,
        )
        if args.command == "deploy":
            release = ReleaseContract.from_json(Path(args.release).read_text(encoding="utf-8"))
            record = controller.deploy(environment, release)
            print(f"Release {record.release.release_id} is known-good.")
        else:
            record = controller.recover(environment, args.release_id)
            print(f"Recovered known-good release {record.release.release_id}.")
        return 0
    except (CommandFailed, ContractError, ReleaseError, OSError, ValueError) as error:
        detail = str(error) if isinstance(error, (ContractError, ReleaseError, ValueError)) else type(error).__name__
        print(f"Release command failed: {detail}", file=sys.stderr)
        return 1


def _local_store() -> LocalDirectoryReleaseStore:
    root = os.environ.get("LOCALAPPDATA")
    if not root:
        raise ReleaseError("LOCALAPPDATA is required for private local release recovery storage.")
    directory = (Path(root) / "WildBunch" / "releases").resolve()
    if directory == REPOSITORY_ROOT or REPOSITORY_ROOT in directory.parents:
        raise ReleaseError("Local release recovery storage must remain outside the repository.")
    return LocalDirectoryReleaseStore(directory)


if __name__ == "__main__":
    sys.exit(main())
class AwsSecretSource:
    def __init__(self, environment: EnvironmentContract, client=None):
        self._client = client or boto3.client("secretsmanager", region_name=environment.region)
        self._arns = {
            "runtime": environment.runtime_secret_arn,
            "migration": environment.migration_secret_arn,
        }

    def get(self, name: str, version_id: str | None = None) -> SecretValue:
        secret_arn = self._arns.get(name)
        if not secret_arn:
            raise ReleaseError("The selected environment has no application secret ARN.")
        request = {"SecretId": secret_arn}
        if version_id is not None:
            request["VersionId"] = version_id
        try:
            result = self._client.get_secret_value(**request)
            value = json.loads(result["SecretString"])
            actual_version = result["VersionId"]
        except Exception:
            raise ReleaseError("Application credentials could not be retrieved; values were suppressed.") from None
        if not isinstance(value, dict) or not isinstance(actual_version, str):
            raise ReleaseError("Application credential metadata is malformed.")
        return SecretValue(value, actual_version)


class LocalSecretSource:
    _names = {"runtime": "runtime-database-credential", "migration": "migration-database-credential"}

    def __init__(self, context: str, namespace: str):
        self._context = context
        self._namespace = namespace

    def get(self, name: str, version_id: str | None = None) -> SecretValue:
        secret_name = self._names.get(name)
        if not secret_name:
            raise ReleaseError("Unknown local database credential.")
        raw = run_checked(
            [
                "kubectl", "--context", self._context, "-n", self._namespace,
                "get", "secret", secret_name, "-o", "json",
            ]
        )
        try:
            data = json.loads(raw)["data"]
            value = {key: base64.b64decode(item, validate=True).decode("utf-8") for key, item in data.items()}
        except (KeyError, TypeError, ValueError, json.JSONDecodeError):
            raise ReleaseError("Local application credential Secret is malformed.") from None
        return SecretValue(value, "local")


class S3ReleaseStore:
    def __init__(self, bucket: str, prefix: str, region: str, client=None):
        self._client = client or boto3.client("s3", region_name=region)
        self._bucket = bucket
        self._prefix = prefix.rstrip("/")

    def _get_json(self, key: str) -> dict[str, object] | None:
        from botocore.exceptions import ClientError

        try:
            response = self._client.get_object(Bucket=self._bucket, Key=key)
        except ClientError as error:
            code = error.response.get("Error", {}).get("Code")
            if code in {"NoSuchKey", "404", "NotFound"}:
                return None
            raise ReleaseError("Private release storage could not be read.") from None
        try:
            value = json.loads(response["Body"].read())
        except (KeyError, TypeError, json.JSONDecodeError):
            raise ReleaseError("Private release storage contains invalid JSON.") from None
        if not isinstance(value, dict):
            raise ReleaseError("Private release storage contains invalid JSON.")
        return value

    def get(self, release_id: str) -> ReleaseRecord | None:
        value = self._get_json(f"{self._prefix}/{release_id}/record.json")
        return None if value is None else ReleaseRecord.from_json(json.dumps(value))

    def save(self, record: ReleaseRecord) -> None:
        self._client.put_object(
            Bucket=self._bucket,
            Key=f"{self._prefix}/{record.release.release_id}/record.json",
            Body=record.to_json().encode("utf-8"),
            ContentType="application/json",
            ServerSideEncryption="AES256",
        )

    def current_release_id(self) -> str | None:
        value = self._get_json(f"{self._prefix}/current.json")
        release_id = None if value is None else value.get("release_id")
        if release_id is not None and not isinstance(release_id, str):
            raise ReleaseError("Private release pointer is invalid.")
        return release_id

    def set_current_release_id(self, release_id: str) -> None:
        self._client.put_object(
            Bucket=self._bucket,
            Key=f"{self._prefix}/current.json",
            Body=json.dumps({"release_id": release_id}, separators=(",", ":")).encode("utf-8"),
            ContentType="application/json",
            ServerSideEncryption="AES256",
        )


class LocalDirectoryReleaseStore:
    def __init__(self, directory: Path):
        self._directory = directory.resolve()
        self._directory.mkdir(parents=True, exist_ok=True)

    def _path(self, release_id: str) -> Path:
        if not re.fullmatch(r"[a-z0-9-]{1,63}", release_id):
            raise ReleaseError("Release ID is invalid for local private storage.")
        return self._directory / f"{release_id}.json"

    def get(self, release_id: str) -> ReleaseRecord | None:
        path = self._path(release_id)
        try:
            return ReleaseRecord.from_json(path.read_text(encoding="utf-8")) if path.exists() else None
        except OSError:
            raise ReleaseError("Local private release storage could not be read.") from None

    def save(self, record: ReleaseRecord) -> None:
        path = self._path(record.release.release_id)
        temporary = path.with_suffix(".tmp")
        try:
            temporary.write_text(record.to_json(), encoding="utf-8")
            os.replace(temporary, path)
        except OSError:
            raise ReleaseError("Local private release storage could not be written.") from None

    def current_release_id(self) -> str | None:
        path = self._directory / "current.json"
        if not path.exists():
            return None
        try:
            release_id = json.loads(path.read_text(encoding="utf-8"))["release_id"]
        except (OSError, KeyError, TypeError, json.JSONDecodeError):
            raise ReleaseError("Local private release pointer is invalid.") from None
        return release_id if isinstance(release_id, str) else None

    def set_current_release_id(self, release_id: str) -> None:
        temporary = self._directory / "current.tmp"
        try:
            temporary.write_text(json.dumps({"release_id": release_id}), encoding="utf-8")
            os.replace(temporary, self._directory / "current.json")
        except OSError:
            raise ReleaseError("Local private release pointer could not be written.") from None


class KubectlReleaseAdapter:
    def __init__(self, context: str, namespace: str, *, local: bool = False, api_readiness_path: str = "/health/ready"):
        if api_readiness_path not in {"/health/ready", "/health/incorrect"} or (not local and api_readiness_path != "/health/ready"):
            raise ReleaseError("Only the local recovery exercise may select the deliberate readiness-path fault.")
        self._context = context
        self._namespace = namespace
        self._local = local
        self._api_readiness_path = api_readiness_path
        self._overlay = REPOSITORY_ROOT / "deploy/kubernetes/overlays/local" if local else REPOSITORY_ROOT / "deploy/kubernetes/overlays/aws"
        self._job_template = REPOSITORY_ROOT / "deploy/kubernetes/jobs/migrate.yaml"

    def _kubectl(self, args: list[str], *, stdin: str | None = None) -> str:
        return run_checked(["kubectl", "--context", self._context, *args], stdin=stdin)

    def _render_objects(self, environment: EnvironmentContract, release: ReleaseContract) -> list[dict[str, object]]:
        rendered = self._kubectl(["kustomize", str(self._overlay)])
        try:
            objects = list(yaml.safe_load_all(rendered))
        except yaml.YAMLError:
            raise ReleaseError("The checked-in AWS Kubernetes overlay could not be rendered.") from None
        objects = [obj for obj in objects if isinstance(obj, dict) and obj.get("kind") not in {"Namespace", "Role", "RoleBinding"}]
        if self._local:
            objects = [
                obj for obj in objects
                if obj.get("metadata", {}).get("name") != "postgres"
                and obj.get("kind") not in {"StatefulSet", "PersistentVolumeClaim"}
            ]
        config = next((obj for obj in objects if obj.get("kind") == "ConfigMap" and obj["metadata"]["name"] == "api-config"), None)
        if config is None:
            raise ReleaseError("The AWS overlay is missing the API configuration map.")
        config["metadata"]["name"] = release.config_map_name
        config["metadata"].setdefault("annotations", {})["learning.wildbunch.dev/release-id"] = release.release_id
        config["immutable"] = True
        for deployment in (obj for obj in objects if obj.get("kind") == "Deployment"):
            name = deployment["metadata"]["name"]
            image_name = "api" if name == "api" else "frontend" if name == "frontend" else None
            if image_name is None:
                raise ReleaseError("The AWS overlay contains an unexpected Deployment.")
            template = deployment["spec"]["template"]
            template["metadata"].setdefault("annotations", {})["learning.wildbunch.dev/release-id"] = release.release_id
            container = template["spec"]["containers"][0]
            container["image"] = release.images[image_name]
            if image_name == "api":
                container["envFrom"] = [{"configMapRef": {"name": release.config_map_name}}]
                container["readinessProbe"]["httpGet"]["path"] = self._api_readiness_path
                for variable in container.get("env", []):
                    if variable.get("name") == "ConnectionStrings__WildBunchPostgresDb":
                        variable["valueFrom"]["secretKeyRef"]["name"] = release.runtime_secret_name
        for obj in objects:
            if obj.get("metadata", {}).get("namespace") == "wild-bunch-learning":
                obj["metadata"]["namespace"] = environment.namespace
        return objects

    def render_manifest(self, environment: EnvironmentContract, release: ReleaseContract) -> str:
        objects = self._render_objects(environment, release)
        return yaml.safe_dump_all(objects, sort_keys=False, explicit_start=True)

    def _ensure_config_map(self, name: str, data: dict[str, str]) -> None:
        args = ["-n", self._namespace, "get", "configmap", name, "--ignore-not-found", "-o", "json"]
        existing = self._kubectl(args)
        if existing.strip():
            try:
                value = json.loads(existing)
            except json.JSONDecodeError:
                raise ReleaseError("Existing release ConfigMap is malformed.") from None
            if value.get("immutable") is not True or value.get("data") != data:
                raise ReleaseError("Release ConfigMap name is already bound to different immutable data.")
            return
        manifest = {"apiVersion": "v1", "kind": "ConfigMap", "metadata": {"name": name, "namespace": self._namespace}, "immutable": True, "data": data}
        self._kubectl(["-n", self._namespace, "create", "-f", "-"], stdin=json.dumps(manifest, separators=(",", ":")))

    def _ensure_secret(self, name: str, connection: str) -> None:
        expected = {"connectionString": base64.b64encode(connection.encode("utf-8")).decode("ascii")}
        existing = self._kubectl(["-n", self._namespace, "get", "secret", name, "--ignore-not-found", "-o", "json"])
        if existing.strip():
            try:
                value = json.loads(existing)
            except json.JSONDecodeError:
                raise ReleaseError("Existing release credential Secret is malformed.") from None
            if value.get("immutable") is not True or value.get("data") != expected:
                raise ReleaseError("Release Secret name is already bound to different immutable credential data.")
            return
        manifest = {
            "apiVersion": "v1", "kind": "Secret", "type": "Opaque", "immutable": True,
            "metadata": {"name": name, "namespace": self._namespace}, "data": expected,
        }
        self._kubectl(["-n", self._namespace, "create", "-f", "-"], stdin=json.dumps(manifest, separators=(",", ":")))

    def prepare_release_objects(self, environment, release, runtime_connection, migration_connection):
        config = next(
            obj for obj in self._render_objects(environment, release)
            if obj.get("kind") == "ConfigMap" and obj["metadata"]["name"] == release.config_map_name
        )
        self._ensure_config_map(release.config_map_name, config["data"])
        self._ensure_secret(release.runtime_secret_name, runtime_connection)
        self._ensure_secret(release.migration_secret_name, migration_connection)

    def job_state(self, name: str) -> JobState:
        raw = self._kubectl(["-n", self._namespace, "get", "job", name, "--ignore-not-found", "-o", "json"])
        if not raw.strip():
            return JobState.ABSENT
        try:
            conditions = json.loads(raw).get("status", {}).get("conditions", [])
        except (AttributeError, json.JSONDecodeError):
            raise ReleaseError("Migration Job status could not be read safely.") from None
        for condition in conditions:
            if condition.get("status") == "True" and condition.get("type") == "Failed":
                return JobState.FAILED
            if condition.get("status") == "True" and condition.get("type") == "Complete":
                return JobState.COMPLETE
        return JobState.RUNNING

    def create_migration_job(
        self, name: str, image: str, secret_name: str, release_id: str, source_sha: str
    ) -> None:
        try:
            manifest = json.loads(self._job_template.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            raise ReleaseError("The checked-in migration Job template is invalid.") from None
        manifest["metadata"]["name"] = name
        manifest["metadata"]["namespace"] = self._namespace
        manifest["metadata"].setdefault("annotations", {})["learning.wildbunch.dev/release-id"] = release_id
        manifest["metadata"].setdefault("labels", {})["learning.wildbunch.dev/release-prefix"] = release_id[:12]
        manifest["metadata"]["annotations"]["learning.wildbunch.dev/source-sha"] = source_sha
        manifest["metadata"].setdefault("labels", {})["learning.wildbunch.dev/release-prefix"] = source_sha[:12]
        pod_metadata = manifest["spec"]["template"]["metadata"]
        pod_metadata.setdefault("annotations", {}).update(manifest["metadata"]["annotations"])
        pod_metadata.setdefault("labels", {})["learning.wildbunch.dev/release-prefix"] = source_sha[:12]
        manifest["spec"]["template"]["spec"]["containers"][0]["image"] = image
        for item in manifest["spec"]["template"]["spec"]["containers"][0]["env"]:
            if item.get("name") == "ConnectionStrings__WildBunchPostgresDb":
                item["valueFrom"]["secretKeyRef"]["name"] = secret_name
        self._kubectl(["-n", self._namespace, "create", "-f", "-"], stdin=json.dumps(manifest, separators=(",", ":")))

    def wait_for_migration_job(self, name: str, timeout_seconds: int) -> bool:
        deadline = time.monotonic() + timeout_seconds
        while time.monotonic() < deadline:
            state = self.job_state(name)
            if state is JobState.COMPLETE:
                return True
            if state is JobState.FAILED:
                return True
            time.sleep(5)
        return False

    def verify_migration_history(self, runtime_secret: SecretValue, environment: EnvironmentContract) -> bool:
        value = runtime_secret.value
        forward = None
        database_host = str(value["host"])
        database_port = int(value["port"])
        if self._local:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
                listener.bind(("127.0.0.1", 0))
                local_port = listener.getsockname()[1]
            forward = subprocess.Popen(
                ["kubectl", "--context", self._context, "-n", self._namespace, "port-forward", "--address", "127.0.0.1", "service/postgres", f"{local_port}:5432"],
                cwd=REPOSITORY_ROOT, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
            )
        try:
            if self._local:
                deadline = time.monotonic() + 15
                while time.monotonic() < deadline:
                    if forward is not None and forward.poll() is not None:
                        return False
                    try:
                        with socket.create_connection(("127.0.0.1", local_port), timeout=0.2):
                            break
                    except OSError:
                        time.sleep(0.1)
                else:
                    return False
                database_host, database_port = "127.0.0.1", local_port
            connection = psycopg.connect(
                host=database_host, port=database_port, dbname=str(value["database"]),
                user=str(value["username"]), password=str(value["password"]), connect_timeout=8,
                sslmode="disable" if self._local else "verify-full",
                sslrootcert=None if self._local else str(REPOSITORY_ROOT / "deploy/database/certs/eu-north-1-bundle.pem"),
            )
            with connection, connection.cursor() as cursor:
                cursor.execute("SELECT to_regclass('public.\"__EFMigrationsHistory\"')")
                if cursor.fetchone()[0] is None:
                    return False
                cursor.execute('SELECT COUNT(*) FROM public."__EFMigrationsHistory"')
                return cursor.fetchone()[0] > 0
        except Exception:
            return False
        finally:
            if forward is not None and forward.poll() is None:
                forward.terminate()
                try:
                    forward.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    forward.kill()
                    forward.wait(timeout=5)

    def apply_workload(self, manifest: str) -> None:
        self._kubectl(["-n", self._namespace, "apply", "-f", "-"], stdin=manifest)

    def _verify_pods(self, deployment: str, expected_image: str) -> None:
        label = "wild-bunch-api" if deployment == "api" else "wild-bunch-frontend"
        for _ in range(48):
            raw = self._kubectl(["-n", self._namespace, "get", "pods", "-l", f"app.kubernetes.io/name={label}", "-o", "json"])
            try:
                pods = json.loads(raw).get("items", [])
            except (AttributeError, json.JSONDecodeError):
                raise ReleaseError("Application Pod status could not be verified.") from None
            ready = []
            for pod in pods:
                statuses = pod.get("status", {}).get("containerStatuses", [])
                containers = pod.get("spec", {}).get("containers", [])
                if pod.get("status", {}).get("phase") == "Running" and statuses and statuses[0].get("ready"):
                    image_id = statuses[0].get("imageID", "")
                    if not containers or containers[0].get("image") != expected_image:
                        continue
                    if "@sha256:" in expected_image and expected_image.split("@sha256:", 1)[1] not in image_id:
                        continue
                    ready.append(pod)
            if ready:
                return
            time.sleep(5)
        raise ReleaseError(f"The {deployment} Deployment did not become ready with its expected image digest.")

    def _http_checks(self, source_sha: str) -> None:
        def free_port() -> int:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
                listener.bind(("127.0.0.1", 0))
                return listener.getsockname()[1]

        frontend_port = free_port()
        api_port = free_port()
        forwards = [
            subprocess.Popen(
                ["kubectl", "--context", self._context, "-n", self._namespace, "port-forward", "--address", "127.0.0.1", "service/frontend", f"{frontend_port}:8080"],
                cwd=REPOSITORY_ROOT, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
            ),
            subprocess.Popen(
                ["kubectl", "--context", self._context, "-n", self._namespace, "port-forward", "--address", "127.0.0.1", "service/api", f"{api_port}:8080"],
                cwd=REPOSITORY_ROOT, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
            ),
        ]
        try:
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline:
                if any(process.poll() is not None for process in forwards):
                    raise ReleaseError("Loopback frontend verification could not start.")
                try:
                    with urllib.request.urlopen(f"http://127.0.0.1:{frontend_port}/health", timeout=2) as response:
                        frontend_ready = response.status == 200
                except (OSError, urllib.error.URLError):
                    frontend_ready = False
                try:
                    with socket.create_connection(("127.0.0.1", api_port), timeout=0.2):
                        api_forward_ready = True
                except OSError:
                    api_forward_ready = False
                if frontend_ready and api_forward_ready:
                    break
                time.sleep(0.25)
            else:
                raise ReleaseError("Loopback frontend health check timed out.")
            for path in ("/",):
                try:
                    with urllib.request.urlopen(f"http://127.0.0.1:{frontend_port}{path}", timeout=8) as response:
                        if response.status != 200:
                            raise ReleaseError("HTTP release verification failed.")
                        if path == "/" and response.headers.get("X-Wild-Bunch-Release") != source_sha:
                            raise ReleaseError("Frontend release identity does not match the selected source revision.")
                except (OSError, urllib.error.URLError):
                    raise ReleaseError("HTTP release verification failed.") from None
            try:
                with urllib.request.urlopen(f"http://127.0.0.1:{api_port}/health/ready", timeout=8) as response:
                    if response.status != 200:
                        raise ReleaseError("API database-readiness verification failed.")
            except (OSError, urllib.error.URLError):
                raise ReleaseError("API database-readiness verification failed.") from None
        finally:
            for process in forwards:
                if process.poll() is None:
                    process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)

    def verify_workload(self, release: ReleaseContract, manifest: str) -> None:
        for name in ("api", "frontend"):
            self._kubectl(["-n", self._namespace, "rollout", "status", f"deployment/{name}", "--timeout=240s"])
            self._verify_pods(name, release.images[name])
        self._http_checks(release.source_sha)
