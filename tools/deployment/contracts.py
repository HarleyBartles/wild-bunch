"""Non-secret infrastructure and application release contracts."""

from __future__ import annotations

import json
import re
from dataclasses import asdict, dataclass
from typing import Any


_SOURCE_SHA = re.compile(r"^[0-9a-f]{40}$")
_AWS_IMAGE = re.compile(r"^[a-z0-9.-]+\.dkr\.ecr\.[a-z0-9-]+\.amazonaws\.com/[a-z0-9._/-]+@sha256:[0-9a-f]{64}$")
_LOCAL_RELEASE_ID = re.compile(r"^local-([0-9a-f]{40})-([0-9]+)$")
_IMAGE_NAMES = frozenset({"frontend", "api", "migrations"})


class ContractError(ValueError):
    """A contract is incomplete, inconsistent or belongs to another environment."""


@dataclass(frozen=True)
class EnvironmentContract:
    region: str
    owner_id: str
    cluster_name: str
    namespace: str
    kubernetes_context: str
    image_repositories: dict[str, str]
    runtime_secret_arn: str | None = None
    migration_secret_arn: str | None = None
    admin_secret_arn: str | None = None
    database_host: str | None = None
    database_port: int | None = None
    database_ca_identifier: str | None = None
    initializer_project: str | None = None
    release_project: str | None = None
    release_bucket: str | None = None
    release_prefix: str | None = None

    def to_json(self) -> str:
        return json.dumps(asdict(self), sort_keys=True, separators=(",", ":"))

    @classmethod
    def from_json(cls, value: str) -> EnvironmentContract:
        data = _parse_json_object(value, cls)
        data["image_repositories"] = _image_mapping(data.get("image_repositories"))
        return cls(**data)


@dataclass(frozen=True)
class ReleaseContract:
    owner_id: str
    release_id: str
    source_sha: str
    images: dict[str, str]
    config_map_name: str
    runtime_secret_name: str
    migration_secret_name: str
    recovery_release_id: str | None
    compatibility_acknowledgement: str

    def to_json(self) -> str:
        return json.dumps(asdict(self), sort_keys=True, separators=(",", ":"))

    @classmethod
    def from_json(cls, value: str) -> ReleaseContract:
        data = _parse_json_object(value, cls)
        data["images"] = _image_mapping(data.get("images"))
        return cls(**data)


def _parse_json_object(value: str, contract_type: type[Any]) -> dict[str, Any]:
    try:
        data = json.loads(value)
    except json.JSONDecodeError as error:
        raise ContractError("Contract must be valid JSON.") from error
    if not isinstance(data, dict):
        raise ContractError("Contract must be a JSON object.")
    expected = set(contract_type.__dataclass_fields__)
    if set(data) != expected:
        raise ContractError("Contract has missing or unknown fields.")
    return data


def _image_mapping(value: Any) -> dict[str, str]:
    if not isinstance(value, dict) or set(value) != _IMAGE_NAMES:
        raise ContractError("Contract must identify exactly the frontend, API and migration images.")
    if any(not isinstance(reference, str) or not reference for reference in value.values()):
        raise ContractError("Image references must be non-empty strings.")
    return value


def validate_contracts(
    environment: EnvironmentContract,
    release: ReleaseContract,
    *,
    expected_context: str,
    expected_region: str,
    expected_owner_id: str,
    expected_namespace: str,
    allow_local_tags: bool,
) -> None:
    for field_name in ("region", "owner_id", "cluster_name", "namespace", "kubernetes_context"):
        field_value = getattr(environment, field_name)
        if not isinstance(field_value, str) or not field_value:
            raise ContractError(f"Environment {field_name} must be a non-empty string.")
    for field_name in (
        "owner_id",
        "release_id",
        "source_sha",
        "config_map_name",
        "runtime_secret_name",
        "migration_secret_name",
        "compatibility_acknowledgement",
    ):
        field_value = getattr(release, field_name)
        if not isinstance(field_value, str) or not field_value:
            raise ContractError(f"Release {field_name} must be a non-empty string.")
    _image_mapping(environment.image_repositories)
    _image_mapping(release.images)
    if release.recovery_release_id is not None and not isinstance(release.recovery_release_id, str):
        raise ContractError("Release recovery_release_id must be a string or null.")
    if environment.kubernetes_context != expected_context:
        raise ContractError("Kubernetes context does not match the selected environment.")
    if environment.region != expected_region:
        raise ContractError("Environment region does not match the selected region.")
    if environment.namespace != expected_namespace:
        raise ContractError("Environment namespace does not match the selected namespace.")
    if environment.owner_id != expected_owner_id or release.owner_id != environment.owner_id:
        raise ContractError("Environment and release owner IDs do not match.")
    if not environment.namespace or not environment.cluster_name:
        raise ContractError("Environment namespace and cluster name are required.")
    if not _SOURCE_SHA.fullmatch(release.source_sha):
        raise ContractError("Release source SHA must be a full lowercase Git SHA.")
    if not release.compatibility_acknowledgement.strip():
        raise ContractError("Release compatibility acknowledgement is required.")
    if allow_local_tags:
        _validate_local_tags(environment, release, expected_context)
    else:
        _validate_aws_digests(environment, release)


def _validate_local_tags(
    environment: EnvironmentContract,
    release: ReleaseContract,
    expected_context: str,
) -> None:
    if expected_context != "kind-wild-bunch-learning" or environment.owner_id != "local":
        raise ContractError("Local image tags are allowed only in the Wild Bunch learning kind context.")
    local_release = _LOCAL_RELEASE_ID.fullmatch(release.release_id)
    if local_release is None or local_release.group(1) != release.source_sha:
        raise ContractError("Local release ID must contain the full source SHA and a unique numeric suffix.")
    for image_name, repository in environment.image_repositories.items():
        if repository != f"wild-bunch/{image_name}":
            raise ContractError("Local image repository does not match the fixed learning image name.")
        if release.images[image_name] != f"{repository}:{release.source_sha}":
            raise ContractError("Local image tags must use the release source SHA.")


def _validate_aws_digests(environment: EnvironmentContract, release: ReleaseContract) -> None:
    if not re.fullmatch(rf"{release.source_sha}-[0-9]+-[0-9]+", release.release_id):
        raise ContractError("AWS release ID must include the full source SHA and workflow run and attempt.")
    for image_name, repository in environment.image_repositories.items():
        reference = release.images[image_name]
        if not _AWS_IMAGE.fullmatch(reference) or not reference.startswith(f"{repository}@sha256:"):
            raise ContractError(f"AWS {image_name} image must use its repository and a valid sha256 digest.")
