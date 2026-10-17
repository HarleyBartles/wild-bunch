from __future__ import annotations

import unittest
from dataclasses import replace

from tools.deployment.contracts import EnvironmentContract
from tools.deployment.inventory import AwsInventorySource, InventoryError, OwnershipError, Resource, inspect


def environment() -> EnvironmentContract:
    return EnvironmentContract(
        region="eu-north-1",
        owner_id="wild-bunch-learning",
        cluster_name="wild-bunch-learning",
        namespace="wild-bunch-learning",
        kubernetes_context="arn:aws:eks:eu-north-1:123456789012:cluster/wild-bunch-learning",
        image_repositories={"frontend": "123456789012.dkr.ecr.eu-north-1.amazonaws.com/wild-bunch-frontend", "api": "123456789012.dkr.ecr.eu-north-1.amazonaws.com/wild-bunch-api", "migrations": "123456789012.dkr.ecr.eu-north-1.amazonaws.com/wild-bunch-migrations"},
        runtime_secret_arn="arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/runtime",
        migration_secret_arn="arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/migration",
        admin_secret_arn="arn:aws:secretsmanager:eu-north-1:123456789012:secret:rds!db-123",
        release_bucket="wild-bunch-learning-state",
    )


class StaticSource:
    def __init__(self, resources: list[Resource] | None = None, error: Exception | None = None) -> None:
        self.resources = resources or []
        self.error = error

    def inspect(self, contract: EnvironmentContract) -> list[Resource]:
        if self.error:
            raise self.error
        return self.resources


class FakePaginator:
    def __init__(self, pages: list[dict[str, object]], error: Exception | None = None) -> None:
        self.pages = pages
        self.error = error

    def paginate(self, **kwargs: object) -> list[dict[str, object]]:
        if self.error:
            raise self.error
        return self.pages


class FakeClient:
    def __init__(self, *, pages: list[dict[str, object]] | None = None, secret: dict[str, object] | None = None, error: Exception | None = None) -> None:
        self.pages = pages or []
        self.secret = secret
        self.error = error
        self.calls: list[str] = []

    def get_paginator(self, operation: str) -> FakePaginator:
        self.calls.append(operation)
        return FakePaginator(self.pages, self.error)

    def describe_secret(self, SecretId: str) -> dict[str, object]:
        self.calls.append("describe_secret")
        if self.error:
            raise self.error
        return self.secret or {"ARN": SecretId}

    def get_bucket_tagging(self, **kwargs: object) -> dict[str, object]:
        return {"TagSet": [{"Key": "Project", "Value": "wild-bunch-learning"}]}

    def describe_repositories(self, **kwargs: object) -> dict[str, object]:
        return {"repositories": []}


class FakeSession:
    def __init__(self, clients: dict[str, FakeClient]) -> None:
        self.clients = clients

    def client(self, service: str, **kwargs: object) -> FakeClient:
        return self.clients[service]


class InventoryTests(unittest.TestCase):
    def test_scheduled_secret_deletion_and_network_resources_remain_outstanding(self) -> None:
        owner = environment().owner_id
        resources = [
            Resource("secretsmanager", "secret", "arn:secret", "scheduled-deletion", owner, {"deleted_date": "future"}),
            Resource("ec2", "nat", "nat-123", "available", owner, {}),
            Resource("ec2", "eip", "eipalloc-123", "associated", owner, {}),
            Resource("logs", "log-group", "/aws/codebuild/wild-bunch-learning", "present", owner, {}),
        ]

        report = inspect(environment(), StaticSource(resources))

        self.assertEqual({item.identifier for item in report.remaining}, {"arn:secret", "nat-123", "eipalloc-123", "/aws/codebuild/wild-bunch-learning"})
        self.assertEqual(report.resources[0].status, "scheduled-deletion")

    def test_aws_adapter_counts_scheduled_secret_and_tagged_nat_eip_and_logs(self) -> None:
        contract = replace(environment(), migration_secret_arn=None, admin_secret_arn=None)
        tagged_resources = []
        for service, kind, name in (("ec2", "natgateway", "nat-123"), ("ec2", "eip", "eipalloc-123"), ("logs", "log-group", "/aws/codebuild/wild-bunch-learning")):
            tagged_resources.append({"ResourceARN": f"arn:aws:{service}:eu-north-1:123456789012:{kind}/{name}", "Tags": [{"Key": "Project", "Value": "wild-bunch-learning"}, {"Key": "OwnerId", "Value": contract.owner_id}]})
        tagging = FakeClient(pages=[{"ResourceTagMappingList": tagged_resources}])
        secret = FakeClient(secret={"ARN": contract.runtime_secret_arn, "DeletedDate": "future"})
        empty = FakeClient()
        clients = {"resourcegroupstaggingapi": tagging, "secretsmanager": secret, "s3": empty, "ecr": empty}
        report = inspect(contract, AwsInventorySource(FakeSession(clients)))

        self.assertEqual({item.kind for item in report.remaining}, {"natgateway", "eip", "log-group", "secret", "bucket"})
        self.assertIn("describe_secret", secret.calls)
        self.assertNotIn("get_secret_value", secret.calls)

    def test_aws_adapter_permission_denial_is_an_inventory_error(self) -> None:
        tagging = FakeClient(error=PermissionError("AccessDenied"))
        clients = {"resourcegroupstaggingapi": tagging, "secretsmanager": FakeClient(), "s3": FakeClient(), "ecr": FakeClient()}
        with self.assertRaisesRegex(InventoryError, "Tagged AWS resource discovery failed"):
            inspect(environment(), AwsInventorySource(FakeSession(clients)))

    def test_aws_adapter_refuses_foreign_owner_tag(self) -> None:
        tagging = FakeClient(pages=[{"ResourceTagMappingList": [{"ResourceARN": "arn:aws:ec2:eu-north-1:123456789012:natgateway/nat-foreign", "Tags": [{"Key": "Project", "Value": "wild-bunch-learning"}, {"Key": "OwnerId", "Value": "another-project"}]}]}])
        clients = {"resourcegroupstaggingapi": tagging, "secretsmanager": FakeClient(), "s3": FakeClient(), "ecr": FakeClient()}
        with self.assertRaisesRegex(OwnershipError, "expected environment owner"):
            inspect(environment(), AwsInventorySource(FakeSession(clients)))

    def test_foreign_resource_ownership_is_refused(self) -> None:
        resource = Resource("ec2", "nat", "nat-foreign", "available", "another-project", {})

        with self.assertRaisesRegex(OwnershipError, "owner does not match"):
            inspect(environment(), StaticSource([resource]))

    def test_permission_failure_cannot_be_reported_as_an_empty_inventory(self) -> None:
        with self.assertRaisesRegex(InventoryError, "no absence conclusion"):
            inspect(environment(), StaticSource(error=PermissionError("AccessDenied")))

    def test_known_absence_is_not_counted_as_remaining(self) -> None:
        resource = Resource("ecr", "repository", "wild-bunch-api", "absent", environment().owner_id, {})

        report = inspect(environment(), StaticSource([resource]))

        self.assertEqual(report.remaining, ())

    def test_local_contract_and_unexpected_region_are_rejected_before_discovery(self) -> None:
        source = StaticSource()
        with self.assertRaisesRegex(InventoryError, "configured AWS owner"):
            inspect(replace(environment(), region="us-east-1"), source)
        self.assertEqual(source.resources, [])


if __name__ == "__main__":
    unittest.main()
