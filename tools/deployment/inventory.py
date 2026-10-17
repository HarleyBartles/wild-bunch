"""Read-only, ownership-checked inventory for the learning environment."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict, dataclass
from typing import Any, Protocol

from .contracts import EnvironmentContract


class InventoryError(RuntimeError):
    """Discovery failed, so absence cannot be asserted."""


class OwnershipError(InventoryError):
    """A discovered resource does not belong to the learning exercise."""


@dataclass(frozen=True)
class Resource:
    service: str
    kind: str
    identifier: str
    status: str
    owner_id: str
    details: dict[str, Any]

    @property
    def remaining(self) -> bool:
        return self.status not in {"absent", "deleted", "terminated", "removed"}


@dataclass(frozen=True)
class Inventory:
    owner_id: str
    region: str
    resources: tuple[Resource, ...]

    @property
    def remaining(self) -> tuple[Resource, ...]:
        return tuple(resource for resource in self.resources if resource.remaining)

    def to_json(self) -> str:
        return json.dumps({"owner_id": self.owner_id, "region": self.region, "resources": [asdict(item) | {"remaining": item.remaining} for item in self.resources]}, indent=2, sort_keys=True)


class InventorySource(Protocol):
    def inspect(self, environment: EnvironmentContract) -> list[Resource]: ...


def inspect(environment: EnvironmentContract, source: InventorySource) -> Inventory:
    if environment.owner_id in {"", "local"} or environment.region != "eu-north-1":
        raise InventoryError("Inventory requires the configured AWS owner and eu-north-1 environment contract.")
    try:
        resources = source.inspect(environment)
    except InventoryError:
        raise
    except Exception as error:
        raise InventoryError(f"AWS inventory discovery failed ({type(error).__name__}); no absence conclusion is available.") from error
    for resource in resources:
        if resource.owner_id != environment.owner_id:
            raise OwnershipError(f"Refusing resource {resource.identifier}: owner does not match the environment contract.")
    return Inventory(environment.owner_id, environment.region, tuple(resources))


class AwsInventorySource:
    """Uses tagged discovery and exact contract identifiers; never reads secret values or object bodies."""

    def __init__(self, session: Any | None = None) -> None:
        try:
            import boto3
        except ImportError as error:
            raise InventoryError("Install tools/deployment/requirements.txt to use AWS inventory.") from error
        self.session = session or boto3.Session()

    def inspect(self, environment: EnvironmentContract) -> list[Resource]:
        client = self.session.client("resourcegroupstaggingapi", region_name=environment.region)
        bootstrap_role_names = {
            "wild-bunch-learning-image-publisher",
            "wild-bunch-learning-database-init-invoker",
            "wild-bunch-learning-infrastructure",
        }
        tagged: list[Resource] = []
        paginator = client.get_paginator("get_resources")
        try:
            pages = paginator.paginate(TagFilters=[{"Key": "Project", "Values": ["wild-bunch-learning"]}])
            for page in pages:
                for item in page.get("ResourceTagMappingList", []):
                    tags = {tag["Key"]: tag["Value"] for tag in item.get("Tags", [])}
                    arn = item["ResourceARN"]
                    resource_name = arn.rsplit("/", 1)[-1]
                    bootstrap_owned = _service_for_arn(arn) == "iam" and resource_name in bootstrap_role_names
                    if tags.get("Project") != "wild-bunch-learning" or (tags.get("OwnerId") != environment.owner_id and not bootstrap_owned):
                        raise OwnershipError("Tagged-resource discovery found a project-tagged resource without the expected environment owner or exact bootstrap role name.")
                    tagged.append(Resource(_service_for_arn(arn), _kind_for_arn(arn), arn, "present", environment.owner_id, {"ownership_basis": "Project and OwnerId tags" if tags.get("OwnerId") else "Project tag and exact bootstrap role name"}))
        except OwnershipError:
            raise
        except Exception as error:
            raise InventoryError(f"Tagged AWS resource discovery failed ({type(error).__name__}).") from error

        found = {resource.identifier for resource in tagged}
        secrets = self.session.client("secretsmanager", region_name=environment.region)
        for arn in (environment.runtime_secret_arn, environment.migration_secret_arn, environment.admin_secret_arn):
            if not arn:
                continue
            try:
                secret = secrets.describe_secret(SecretId=arn)
            except Exception as error:
                if _aws_code(error) in {"ResourceNotFoundException", "ResourceNotFound"}:
                    tagged.append(Resource("secretsmanager", "secret", arn, "absent", environment.owner_id, {"ownership_basis": "exact environment contract ARN"}))
                    continue
                raise InventoryError(f"Secret metadata discovery failed ({type(error).__name__}); secret absence is unknown.") from error
            if secret.get("ARN") != arn:
                raise OwnershipError("Secrets Manager returned an ARN different from the exact environment contract.")
            status = "scheduled-deletion" if secret.get("DeletedDate") else "present"
            tagged.append(Resource("secretsmanager", "secret", arn, status, environment.owner_id, {"ownership_basis": "exact environment contract ARN", "deleted_date": str(secret["DeletedDate"]) if secret.get("DeletedDate") else None}))

        if environment.release_bucket:
            s3 = self.session.client("s3", region_name=environment.region)
            try:
                tags = {item["Key"]: item["Value"] for item in s3.get_bucket_tagging(Bucket=environment.release_bucket).get("TagSet", [])}
                if tags.get("Project") != "wild-bunch-learning":
                    raise OwnershipError("The exact contract bucket does not carry the expected project ownership tag.")
                tagged.append(Resource("s3", "bucket", environment.release_bucket, "present", environment.owner_id, {"ownership_basis": "exact environment contract plus Project tag"}))
                versions = s3.get_paginator("list_object_versions")
                for page in versions.paginate(Bucket=environment.release_bucket):
                    for kind in ("Versions", "DeleteMarkers"):
                        for item in page.get(kind, []):
                            key = item.get("Key", "")
                            if not key.startswith(("plans/", "releases/", "environment/", "bootstrap/")):
                                continue
                            version_id = item["VersionId"]
                            tagged.append(Resource("s3", "delete-marker" if kind == "DeleteMarkers" else "object-version", f"s3://{environment.release_bucket}/{key}?versionId={version_id}", "present", environment.owner_id, {"ownership_basis": "exact contract bucket and exercise prefix", "is_latest": item.get("IsLatest", False)}))
            except OwnershipError:
                raise
            except Exception as error:
                raise InventoryError(f"S3 object-version discovery failed ({type(error).__name__}); bucket cleanup is unknown.") from error

        for repository_url in environment.image_repositories.values():
            repository = repository_url.rsplit("/", 1)[-1]
            ecr = self.session.client("ecr", region_name=environment.region)
            try:
                response = ecr.describe_repositories(repositoryNames=[repository])
            except Exception as error:
                if _aws_code(error) in {"RepositoryNotFoundException"}:
                    tagged.append(Resource("ecr", "repository", repository, "absent", environment.owner_id, {"ownership_basis": "exact environment contract repository"}))
                    continue
                raise InventoryError(f"ECR repository discovery failed ({type(error).__name__}).") from error
            if not response.get("repositories"):
                tagged.append(Resource("ecr", "repository", repository, "absent", environment.owner_id, {"ownership_basis": "exact environment contract repository"}))
            for repo in response.get("repositories", []):
                if repo.get("repositoryName") != repository:
                    raise OwnershipError("ECR returned a repository different from the contract name.")
                tagged.append(Resource("ecr", "repository", repo["repositoryArn"], "present", environment.owner_id, {"ownership_basis": "exact environment contract repository", "name": repository}))
                try:
                    images = ecr.get_paginator("describe_images")
                    for page in images.paginate(repositoryName=repository):
                        for image in page.get("imageDetails", []):
                            tagged.append(Resource("ecr", "image", f"{repo['repositoryArn']}@{image.get('imageDigest', 'unknown')}", "present", environment.owner_id, {"tags": image.get("imageTags", [])}))
                except Exception as error:
                    raise InventoryError(f"ECR image discovery failed ({type(error).__name__}).") from error
        return tagged


def _aws_code(error: Exception) -> str | None:
    return getattr(error, "response", {}).get("Error", {}).get("Code")


def _service_for_arn(arn: str) -> str:
    parts = arn.split(":", 5)
    return parts[2] if len(parts) > 2 else "aws"


def _kind_for_arn(arn: str) -> str:
    resource = arn.split(":", 5)[-1]
    return resource.split("/", 1)[0].split(":", 1)[0]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    inspect_parser = commands.add_parser("inspect", help="Read current AWS inventory for the learning environment")
    inspect_parser.add_argument("--environment", required=True, help="Path to Terraform environment_contract JSON")
    args = parser.parse_args(argv)
    try:
        with open(args.environment, encoding="utf-8") as stream:
            environment = EnvironmentContract.from_json(stream.read())
        report = inspect(environment, AwsInventorySource())
    except (OSError, ValueError, InventoryError) as error:
        print(f"Inventory incomplete: {error}", file=sys.stderr)
        return 2
    print(report.to_json())
    return 1 if report.remaining else 0


if __name__ == "__main__":
    raise SystemExit(main())
