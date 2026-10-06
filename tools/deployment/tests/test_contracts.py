from __future__ import annotations

import unittest
from dataclasses import replace

from tools.deployment.contracts import (
    ContractError,
    EnvironmentContract,
    ReleaseContract,
    validate_contracts,
)


def local_contracts() -> tuple[EnvironmentContract, ReleaseContract]:
    source_sha = "a" * 40
    repositories = {
        "frontend": "wild-bunch/frontend",
        "api": "wild-bunch/api",
        "migrations": "wild-bunch/migrations",
    }
    return (
        EnvironmentContract(
            region="eu-north-1",
            owner_id="local",
            cluster_name="wild-bunch-learning",
            namespace="wild-bunch-learning",
            kubernetes_context="kind-wild-bunch-learning",
            image_repositories=repositories,
        ),
        ReleaseContract(
            owner_id="local",
            release_id=f"local-{source_sha}-123456",
            source_sha=source_sha,
            images={name: f"{repository}:{source_sha}" for name, repository in repositories.items()},
            config_map_name=f"release-{source_sha[:12]}-config",
            runtime_secret_name="local-runtime-credential",
            migration_secret_name="local-migration-credential",
            recovery_release_id=None,
            compatibility_acknowledgement="Local exercise release using the existing schema.",
        ),
    )


class ContractValidationTests(unittest.TestCase):
    def test_accepts_matching_local_image_tags_and_ownership(self) -> None:
        environment, release = local_contracts()

        validate_contracts(
            environment,
            release,
            expected_context="kind-wild-bunch-learning",
            expected_region="eu-north-1",
            expected_owner_id="local",
            expected_namespace="wild-bunch-learning",
            allow_local_tags=True,
        )

    def test_contract_json_round_trips_and_rejects_unknown_fields(self) -> None:
        environment, release = local_contracts()

        self.assertEqual(environment, EnvironmentContract.from_json(environment.to_json()))
        self.assertEqual(release, ReleaseContract.from_json(release.to_json()))
        with self.assertRaisesRegex(ContractError, "unknown fields"):
            EnvironmentContract.from_json(environment.to_json()[:-1] + ',"extra":"value"}')

    def test_rejects_a_different_kubernetes_context(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "Kubernetes context"):
            validate_contracts(
                environment,
                release,
                expected_context="kind-other-cluster",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )

    def test_rejects_a_different_region(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "region"):
            validate_contracts(
                replace(environment, region="us-east-1"),
                release,
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )

    def test_rejects_a_different_namespace(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "namespace"):
            validate_contracts(
                replace(environment, namespace="another-project"),
                release,
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )

    def test_rejects_malformed_aws_digest(self) -> None:
        environment, release = local_contracts()
        aws_environment = replace(
            environment,
            owner_id="aws-learning",
            kubernetes_context="aws-learning",
            image_repositories={
                name: f"123456789012.dkr.ecr.eu-north-1.amazonaws.com/{name}"
                for name in environment.image_repositories
            },
        )
        aws_release = replace(
            release,
            owner_id="aws-learning",
            release_id=f"{release.source_sha}-123-1",
            images={
                name: f"{repository}@sha256:bad"
                for name, repository in aws_environment.image_repositories.items()
            },
        )

        with self.assertRaisesRegex(ContractError, "digest"):
            validate_contracts(
                aws_environment,
                aws_release,
                expected_context="aws-learning",
                expected_region="eu-north-1",
                expected_owner_id="aws-learning",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=False,
            )

    def test_rejects_mixed_environment_and_release_owners(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "owner"):
            validate_contracts(
                environment,
                replace(release, owner_id="another-owner"),
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )


if __name__ == "__main__":
    unittest.main()
