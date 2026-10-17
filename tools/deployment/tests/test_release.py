from __future__ import annotations

import base64
import json
import unittest
from dataclasses import replace

import yaml

from tools.deployment.contracts import EnvironmentContract, ReleaseContract
from tools.deployment.release import (
    JobState,
    KubectlReleaseAdapter,
    ReleaseController,
    ReleaseError,
    ReleaseRecord,
    SecretValue,
)


SOURCE_SHA = "a" * 40
REPOSITORIES = {
    "frontend": "wild-bunch/frontend",
    "api": "wild-bunch/api",
    "migrations": "wild-bunch/migrations",
}


def contracts(release_id: str = f"local-{SOURCE_SHA}-1", recovery: str | None = None):
    environment = EnvironmentContract(
        region="eu-north-1",
        owner_id="local",
        cluster_name="wild-bunch-learning",
        namespace="wild-bunch-learning",
        kubernetes_context="kind-wild-bunch-learning",
        image_repositories=REPOSITORIES,
    )
    release = ReleaseContract(
        owner_id="local",
        release_id=release_id,
        source_sha=SOURCE_SHA,
        images={name: f"{repository}:{SOURCE_SHA}" for name, repository in REPOSITORIES.items()},
        config_map_name=f"api-config-{release_id}",
        runtime_secret_name=f"runtime-{release_id}",
        migration_secret_name=f"migration-{release_id}",
        recovery_release_id=recovery,
        compatibility_acknowledgement=(
            f"schema: current; constraints: unchanged; data/events: compatible; writes: compatible; recovery: {recovery}"
            if recovery
            else "schema: current; constraints: unchanged; data/events: compatible; writes: compatible; recovery: none"
        ),
    )
    return environment, release


class FakeSecrets:
    def __init__(self):
        self.calls: list[tuple[str, str | None]] = []
        self.values = {
            "runtime": SecretValue(
                {"username": "runtime", "password": "runtime-pass", "host": "db.internal", "port": 5432, "database": "wildbunch"},
                "runtime-v1",
            ),
            "migration": SecretValue(
                {"username": "migration", "password": "migration-pass", "host": "db.internal", "port": 5432, "database": "wildbunch"},
                "migration-v1",
            ),
        }
        self.fail = False

    def get(self, name: str, version_id: str | None = None) -> SecretValue:
        self.calls.append((name, version_id))
        if self.fail:
            raise RuntimeError("credential-must-not-leak")
        return self.values[name]


class FakeStore:
    def __init__(self):
        self.records: dict[str, ReleaseRecord] = {}
        self.current: str | None = None
        self.fail_pointer_once = False

    def get(self, release_id: str) -> ReleaseRecord | None:
        return self.records.get(release_id)

    def save(self, record: ReleaseRecord) -> None:
        self.records[record.release.release_id] = record

    def current_release_id(self) -> str | None:
        return self.current

    def set_current_release_id(self, release_id: str) -> None:
        self.current = release_id
        if self.fail_pointer_once:
            self.fail_pointer_once = False
            raise RuntimeError("pointer write acknowledgement lost")


class FakeKubernetes:
    def __init__(self):
        self.calls: list[tuple] = []
        self.job = JobState.ABSENT
        self.job_wait_succeeds = True
        self.migration_verification_succeeds = True
        self.rollout_succeeds = True
        self.secret_creation_succeeds = True

    def prepare_release_objects(self, environment, release, runtime_connection, migration_connection):
        self.calls.append(("prepare", release.release_id))
        if not self.secret_creation_succeeds:
            raise RuntimeError("Kubernetes secret create failed")

    def create_config_map(self, name, data):
        self.calls.append(("config", name, data))

    def create_secret(self, name, value):
        self.calls.append(("secret", name))
        if not self.secret_creation_succeeds:
            raise RuntimeError("Kubernetes secret create failed")

    def job_state(self, name):
        self.calls.append(("job-state", name))
        return self.job

    def create_migration_job(self, name, image, secret_name, release_id, source_sha):
        self.calls.append(("job-create", name, image, secret_name, release_id, source_sha))
        self.job = JobState.RUNNING

    def wait_for_migration_job(self, name, timeout_seconds):
        self.calls.append(("job-wait", name, timeout_seconds))
        if self.job_wait_succeeds:
            self.job = JobState.COMPLETE
        return self.job_wait_succeeds

    def verify_migration_history(self, runtime_secret, environment):
        self.calls.append(("verify-migrations",))
        return self.migration_verification_succeeds

    def apply_workload(self, manifest):
        self.calls.append(("apply-workload", manifest))

    def verify_workload(self, release, manifest):
        self.calls.append(("verify-workload", release.release_id))
        if not self.rollout_succeeds:
            raise RuntimeError("rollout did not become ready")


class ReleaseControllerTests(unittest.TestCase):
    def setUp(self):
        self.environment, self.release = contracts()
        self.kubernetes = FakeKubernetes()
        self.secrets = FakeSecrets()
        self.store = FakeStore()
        self.controller = ReleaseController(
            self.kubernetes,
            self.secrets,
            self.store,
            render_workload=lambda _environment, release: f"full manifest for {release.release_id}",
            expected_context="kind-wild-bunch-learning",
            expected_owner_id="local",
            allow_local_tags=True,
        )

    def test_migration_failure_never_applies_the_application(self):
        self.kubernetes.job = JobState.FAILED

        with self.assertRaisesRegex(ReleaseError, "Migration Job.*failed"):
            self.controller.deploy(self.environment, self.release)

        self.assertFalse(any(call[0] == "apply-workload" for call in self.kubernetes.calls))
        self.assertIsNone(self.store.current)

    def test_timeout_reports_the_existing_job_and_never_creates_a_duplicate(self):
        self.kubernetes.job = JobState.RUNNING
        self.kubernetes.job_wait_succeeds = False

        with self.assertRaisesRegex(ReleaseError, "(?i)migration Job.*uncertain") as raised:
            self.controller.deploy(self.environment, self.release)

        self.assertIn("migration-", str(raised.exception))
        self.assertFalse(any(call[0] == "job-create" for call in self.kubernetes.calls))
        self.assertFalse(any(call[0] == "apply-workload" for call in self.kubernetes.calls))

    def test_completed_existing_migration_job_continues_without_creating_another(self):
        self.kubernetes.job = JobState.COMPLETE

        record = self.controller.deploy(self.environment, self.release)

        self.assertEqual(record.status, "known-good")
        self.assertFalse(any(call[0] == "job-create" for call in self.kubernetes.calls))
        self.assertTrue(any(call[0] == "apply-workload" for call in self.kubernetes.calls))

    def test_failed_rollout_does_not_advance_the_known_good_release(self):
        self.store.current = "known-good"
        self.kubernetes.rollout_succeeds = False
        self.release = replace(
            self.release,
            recovery_release_id="known-good",
            compatibility_acknowledgement=(
                "schema: current; constraints: unchanged; data/events: compatible; "
                "writes: compatible; recovery: known-good"
            ),
        )

        with self.assertRaisesRegex(ReleaseError, "previous known-good"):
            self.controller.deploy(self.environment, self.release)

        self.assertEqual(self.store.current, "known-good")
        self.assertEqual(self.store.records[self.release.release_id].status, "failed")

    def test_recovery_replays_the_complete_stored_manifest_and_secret_versions(self):
        old_environment, old_release = contracts("local-" + SOURCE_SHA + "-9")
        old_record = ReleaseRecord(
            release=old_release,
            manifest="all workload resources, images, config and secret references",
            secret_version_ids={"runtime": "runtime-v1", "migration": "migration-v1"},
            status="known-good",
        )
        self.store.records[old_release.release_id] = old_record
        self.store.current = self.release.release_id

        self.controller.recover(old_environment, old_release.release_id)

        applied = [call[1] for call in self.kubernetes.calls if call[0] == "apply-workload"]
        self.assertEqual(applied, [old_record.manifest])
        self.assertEqual(self.store.current, old_release.release_id)
        self.assertIn(("runtime", "runtime-v1"), self.secrets.calls)
        self.assertIn(("migration", "migration-v1"), self.secrets.calls)

    def test_secret_failure_emits_no_credentials_and_cannot_mark_release_known_good(self):
        self.kubernetes.secret_creation_succeeds = False

        with self.assertRaises(ReleaseError) as raised:
            self.controller.deploy(self.environment, self.release)

        self.assertNotIn("runtime-pass", str(raised.exception))
        self.assertNotIn("migration-pass", str(raised.exception))
        self.assertIsNone(self.store.current)
        self.assertNotEqual(self.store.records[self.release.release_id].status, "known-good")

    def test_secret_retrieval_failure_is_suppressed_and_cannot_persist_credentials(self):
        self.secrets.fail = True

        with self.assertRaises(ReleaseError) as raised:
            self.controller.deploy(self.environment, self.release)

        self.assertNotIn("credential-must-not-leak", str(raised.exception))
        self.assertFalse(self.store.records)
        self.assertFalse(self.kubernetes.calls)

    def test_workload_renderer_uses_unique_immutable_configuration_and_secret_references(self):
        self.kubernetes.rollout_succeeds = True
        self.controller.deploy(self.environment, self.release)
        record = self.store.records[self.release.release_id]
        self.assertIn(self.release.source_sha, record.manifest)
        self.assertNotIn("connectionString", record.manifest)

    def test_connection_string_quotes_delimiters_and_uses_the_region_ca_for_aws(self):
        environment = replace(
            self.environment,
            owner_id="wild-bunch-learning",
            database_host="db.eu-north-1.rds.amazonaws.com",
        )
        value = SecretValue(
            {"username": "wild'bunch", "password": "p;ass'word", "host": environment.database_host, "port": 5432, "database": "wildbunch"},
            "version-1",
        )
        from tools.deployment.release import _connection_string

        connection = _connection_string(value, environment)
        self.assertIn("Username='wild''bunch'", connection)
        self.assertIn("Password='p;ass''word'", connection)
        self.assertIn("SSL Mode=VerifyFull;Root Certificate=/app/certs/rds-ca-bundle.pem", connection)

    def test_release_record_round_trip_contains_only_secret_version_identifiers(self):
        record = ReleaseRecord(self.release, "apiVersion: v1", {"runtime": "r1", "migration": "m1"}, "prepared")

        decoded = ReleaseRecord.from_json(record.to_json())

        self.assertEqual(decoded, record)
        self.assertNotIn("runtime-pass", decoded.to_json())

    def test_renderer_excludes_namespace_privileges_and_changes_both_application_images(self):
        adapter = KubectlReleaseAdapter(self.environment.kubernetes_context, self.environment.namespace, local=True)
        adapter._kubectl = lambda _args: """---
apiVersion: v1
kind: Namespace
metadata: {name: wild-bunch-learning}
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata: {name: release}
---
apiVersion: v1
kind: ConfigMap
metadata: {name: api-config}
data: {ASPNETCORE_ENVIRONMENT: Production}
---
apiVersion: v1
kind: Service
metadata: {name: frontend, namespace: wild-bunch-learning}
---
apiVersion: apps/v1
kind: Deployment
metadata: {name: api, namespace: wild-bunch-learning}
spec:
  template:
    metadata: {labels: {app: api}}
    spec:
      containers:
        - name: api
          envFrom: [{configMapRef: {name: api-config}}]
          readinessProbe: {httpGet: {path: /health/ready, port: http}}
          env:
            - name: ConnectionStrings__WildBunchPostgresDb
              valueFrom: {secretKeyRef: {name: old-runtime, key: connectionString}}
---
apiVersion: apps/v1
kind: Deployment
metadata: {name: frontend, namespace: wild-bunch-learning}
spec:
  template:
    metadata: {labels: {app: frontend}}
    spec:
      containers: [{name: frontend}]
"""

        objects = adapter._render_objects(self.environment, self.release)
        kinds = [item["kind"] for item in objects]
        config = next(item for item in objects if item["kind"] == "ConfigMap")
        api = next(item for item in objects if item["kind"] == "Deployment" and item["metadata"]["name"] == "api")
        frontend = next(item for item in objects if item["kind"] == "Deployment" and item["metadata"]["name"] == "frontend")

        self.assertNotIn("Namespace", kinds)
        self.assertNotIn("Role", kinds)
        self.assertTrue(config["immutable"])
        self.assertEqual(config["metadata"]["name"], self.release.config_map_name)
        self.assertEqual(api["spec"]["template"]["spec"]["containers"][0]["image"], self.release.images["api"])
        self.assertEqual(frontend["spec"]["template"]["spec"]["containers"][0]["image"], self.release.images["frontend"])
        self.assertEqual(api["spec"]["template"]["spec"]["containers"][0]["envFrom"][0]["configMapRef"]["name"], self.release.config_map_name)
        self.assertEqual(api["spec"]["template"]["spec"]["containers"][0]["env"][0]["valueFrom"]["secretKeyRef"]["name"], self.release.runtime_secret_name)

    def test_incorrect_readiness_path_is_available_only_for_the_local_recovery_exercise(self):
        adapter = KubectlReleaseAdapter(
            self.environment.kubernetes_context,
            self.environment.namespace,
            local=True,
            api_readiness_path="/health/incorrect",
        )
        adapter._kubectl = lambda _args: """---
apiVersion: v1
kind: ConfigMap
metadata: {name: api-config}
data: {ASPNETCORE_ENVIRONMENT: Production}
---
apiVersion: apps/v1
kind: Deployment
metadata: {name: api, namespace: wild-bunch-learning}
spec:
  template:
    metadata: {}
    spec:
      containers:
        - name: api
          readinessProbe: {httpGet: {path: /health/ready, port: http}}
          env: []
---
apiVersion: apps/v1
kind: Deployment
metadata: {name: frontend, namespace: wild-bunch-learning}
spec:
  template:
    metadata: {}
    spec:
      containers: [{name: frontend}]
"""
        objects = adapter._render_objects(self.environment, self.release)
        api = next(item for item in objects if item.get("kind") == "Deployment")
        self.assertEqual(api["spec"]["template"]["spec"]["containers"][0]["readinessProbe"]["httpGet"]["path"], "/health/incorrect")
        with self.assertRaisesRegex(ReleaseError, "local recovery exercise"):
            KubectlReleaseAdapter(self.environment.kubernetes_context, self.environment.namespace, local=False, api_readiness_path="/health/incorrect")

    def test_immutable_secret_is_created_from_stdin_without_echoing_credential_values(self):
        adapter = KubectlReleaseAdapter(self.environment.kubernetes_context, self.environment.namespace, local=True)
        invocations = []

        def fake_kubectl(args, *, stdin=None):
            invocations.append((args, stdin))
            return "" if "get" in args else "secret created"

        adapter._kubectl = fake_kubectl
        credential = "password-with-a-semicolon;and-quotes'"
        adapter._ensure_secret("runtime-test", credential)

        self.assertEqual(len(invocations), 2)
        args, payload = invocations[1]
        self.assertEqual(args[-2:], ["-f", "-"])
        self.assertNotIn(credential, payload)
        manifest = json.loads(payload)
        self.assertTrue(manifest["immutable"])
        self.assertEqual(
            base64.b64decode(manifest["data"]["connectionString"]).decode("utf-8"),
            credential,
        )

    def test_existing_immutable_secret_is_never_overwritten_when_values_differ(self):
        adapter = KubectlReleaseAdapter(self.environment.kubernetes_context, self.environment.namespace, local=True)
        previous = {"apiVersion": "v1", "kind": "Secret", "immutable": True, "data": {"connectionString": "old"}}
        invocations = []

        def fake_kubectl(args, *, stdin=None):
            invocations.append((args, stdin))
            return json.dumps(previous)

        adapter._kubectl = fake_kubectl
        with self.assertRaisesRegex(ReleaseError, "different immutable credential data"):
            adapter._ensure_secret("runtime-test", "new-password")

        self.assertEqual(len(invocations), 1)
        self.assertIsNone(invocations[0][1])

    def test_migration_job_template_is_unique_single_attempt_and_uses_migration_secret(self):
        adapter = KubectlReleaseAdapter(self.environment.kubernetes_context, self.environment.namespace, local=True)
        invocation = []
        adapter._kubectl = lambda args, *, stdin=None: invocation.append((args, stdin)) or "job created"

        adapter.create_migration_job(
            "migration-test-release",
            self.release.images["migrations"],
            self.release.migration_secret_name,
            self.release.release_id,
            self.release.source_sha,
        )

        args, payload = invocation[0]
        job = json.loads(payload)
        env = job["spec"]["template"]["spec"]["containers"][0]["env"]
        secret_name = next(item["valueFrom"]["secretKeyRef"]["name"] for item in env if item["name"] == "ConnectionStrings__WildBunchPostgresDb")
        self.assertEqual(args[-2:], ["-f", "-"])
        self.assertEqual(job["spec"]["backoffLimit"], 0)
        self.assertEqual(secret_name, self.release.migration_secret_name)
        self.assertEqual(job["metadata"]["annotations"]["learning.wildbunch.dev/release-id"], self.release.release_id)
        self.assertEqual(job["metadata"]["annotations"]["learning.wildbunch.dev/source-sha"], self.release.source_sha)

    def test_release_role_has_service_update_access_and_no_cluster_scope(self):
        from pathlib import Path

        role = next(
            yaml.safe_load_all(
                (Path(__file__).resolve().parents[3] / "deploy/kubernetes/overlays/aws/release-rbac.yaml").read_text(encoding="utf-8")
            )
        )
        permissions = {
            resource: set(rule["verbs"])
            for rule in role["rules"]
            for resource in rule["resources"]
        }

        self.assertTrue({"get", "create", "patch"}.issubset(permissions["services"]))
        self.assertNotIn("*", permissions)
        self.assertNotIn("cluster-admin", role["metadata"]["name"])

    def test_success_runs_migration_verification_before_application_and_persists_only_secret_versions(self):
        record = self.controller.deploy(self.environment, self.release)

        names = [call[0] for call in self.kubernetes.calls]
        self.assertLess(names.index("job-create"), names.index("verify-migrations"))
        self.assertLess(names.index("verify-migrations"), names.index("apply-workload"))
        self.assertEqual(record.status, "known-good")
        self.assertEqual(record.secret_version_ids, {"runtime": "runtime-v1", "migration": "migration-v1"})
        self.assertNotIn("runtime-pass", repr(record))
        self.assertEqual(self.store.current, self.release.release_id)

    def test_uncertain_known_good_pointer_update_retries_without_a_second_migration_or_rollout(self):
        self.store.fail_pointer_once = True

        with self.assertRaisesRegex(ReleaseError, "pointer update is uncertain"):
            self.controller.deploy(self.environment, self.release)
        first_job_creates = sum(call[0] == "job-create" for call in self.kubernetes.calls)
        first_app_applies = sum(call[0] == "apply-workload" for call in self.kubernetes.calls)

        record = self.controller.deploy(self.environment, self.release)

        self.assertEqual(record.status, "known-good")
        self.assertEqual(sum(call[0] == "job-create" for call in self.kubernetes.calls), first_job_creates)
        self.assertEqual(sum(call[0] == "apply-workload" for call in self.kubernetes.calls), first_app_applies)
        self.assertEqual(self.store.current, self.release.release_id)


if __name__ == "__main__":
    unittest.main()
