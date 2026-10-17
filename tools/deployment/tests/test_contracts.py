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
    release_id = f"local-{source_sha}-123456"
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
            release_id=release_id,
            source_sha=source_sha,
            images={name: f"{repository}:{source_sha}" for name, repository in repositories.items()},
            config_map_name=f"release-{release_id}-config",
            runtime_secret_name=f"local-runtime-{release_id}",
            migration_secret_name=f"local-migration-{release_id}",
            recovery_release_id=None,
            compatibility_acknowledgement=(
                "schema: current; constraints: unchanged; data/events: compatible; writes: compatible; recovery: none"
            ),
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
            config_map_name=f"api-config-{release.source_sha}-123-1",
            runtime_secret_name=f"runtime-{release.source_sha}-123-1",
            migration_secret_name=f"migration-{release.source_sha}-123-1",
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

    def test_requires_distinct_per_release_configuration_and_secret_names(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "unique to this immutable release ID"):
            validate_contracts(
                environment,
                replace(release, runtime_secret_name="runtime-shared"),
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )

    def test_requires_explicit_compatibility_fields_before_release(self) -> None:
        environment, release = local_contracts()

        with self.assertRaisesRegex(ContractError, "stored data/events"):
            validate_contracts(
                environment,
                replace(release, compatibility_acknowledgement="Existing schema works."),
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )

    def test_recovery_compatibility_acknowledgement_identifies_the_known_good_release(self) -> None:
        environment, release = local_contracts()
        recovery_id = f"local-{release.source_sha}-older"

        with self.assertRaisesRegex(ContractError, "identify the selected recovery release"):
            validate_contracts(
                environment,
                replace(release, recovery_release_id=recovery_id),
                expected_context="kind-wild-bunch-learning",
                expected_region="eu-north-1",
                expected_owner_id="local",
                expected_namespace="wild-bunch-learning",
                allow_local_tags=True,
            )


if __name__ == "__main__":
    unittest.main()
