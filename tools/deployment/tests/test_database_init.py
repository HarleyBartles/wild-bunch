from __future__ import annotations

import unittest
from types import SimpleNamespace

from deploy.database.init import (
    BotoSecretStore,
    DatabaseEndpoint,
    ExistingCredentialRejected,
    InitializationNeedsReconciliation,
    initialize_database,
)


class MemorySecretStore:
    def __init__(self, values: dict[str, dict[str, object]] | None = None) -> None:
        self.values = values or {}
        self.create_calls: list[tuple[str, dict[str, object]]] = []
        self.deny_reads = False

    def read(self, name: str) -> dict[str, object] | None:
        if self.deny_reads:
            raise PermissionError("secret access denied")
        return self.values.get(name)

    def create_if_absent(self, name: str, value: dict[str, object]) -> dict[str, object]:
        self.create_calls.append((name, value.copy()))
        return self.values.setdefault(name, value.copy())


class MemoryAdminConnection:
    def __init__(self) -> None:
        self.roles: dict[str, str] = {}
        self.database_owner: str | None = None
        self.fail_database_creation = False
        self.reject_credentials = False
        self.created_roles: list[str] = []
        self.configured = False

    def role_exists(self, username: str) -> bool:
        return username in self.roles

    def authenticate(self, username: str, password: str, database: str) -> None:
        if self.reject_credentials or self.roles.get(username) != password:
            raise ExistingCredentialRejected(username)

    def create_role(self, username: str, password: str) -> None:
        self.roles[username] = password
        self.created_roles.append(username)

    def database_exists(self, database: str) -> bool:
        return self.database_owner is not None

    def create_database(self, database: str, owner: str) -> None:
        if self.fail_database_creation:
            raise RuntimeError("database operation failed")
        self.database_owner = owner

    def configure_privileges(self, database: str, migration_username: str, runtime_username: str) -> None:
        self.configured = True


class FakeSecretsManagerClient:
    class ResourceNotFound(Exception):
        pass

    class ResourceExists(Exception):
        pass

    def __init__(self) -> None:
        self.exceptions = SimpleNamespace(
            ResourceNotFoundException=self.ResourceNotFound,
            ResourceExistsException=self.ResourceExists,
        )
        self.values: dict[str, str] = {}
        self.put_calls: list[dict[str, str]] = []

    def get_secret_value(self, *, SecretId: str) -> dict[str, str]:
        if SecretId not in self.values:
            raise self.exceptions.ResourceNotFoundException()
        return {"SecretString": self.values[SecretId]}

    def put_secret_value(self, *, SecretId: str, SecretString: str) -> None:
        self.put_calls.append({"SecretId": SecretId, "SecretString": SecretString})
        self.values[SecretId] = SecretString


class DatabaseInitializationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.endpoint = DatabaseEndpoint(host="localhost", port=5435)
        self.admin = MemoryAdminConnection()
        self.secrets = MemorySecretStore()

    def test_rerun_preserves_secrets_and_existing_database(self) -> None:
        initialize_database(self.admin, self.secrets, self.endpoint)
        passwords = {name: value["password"] for name, value in self.secrets.values.items()}

        initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual(passwords, {name: value["password"] for name, value in self.secrets.values.items()})
        self.assertEqual(2, len(self.secrets.create_calls))
        self.assertTrue(self.admin.configured)

    def test_secret_created_before_database_failure_is_reused_on_retry(self) -> None:
        self.admin.fail_database_creation = True
        with self.assertRaisesRegex(RuntimeError, "database operation failed"):
            initialize_database(self.admin, self.secrets, self.endpoint)
        passwords = {name: value["password"] for name, value in self.secrets.values.items()}

        self.admin.fail_database_creation = False
        initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual(passwords, {name: value["password"] for name, value in self.secrets.values.items()})

    def test_existing_role_without_its_secret_requires_manual_reconciliation(self) -> None:
        self.admin.roles["wildbunch_runtime"] = "unknown"

        with self.assertRaisesRegex(InitializationNeedsReconciliation, "restore the secret or reconcile the role"):
            initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual([], self.secrets.create_calls)

    def test_existing_role_with_mismatched_secret_is_not_reset(self) -> None:
        self.admin.roles["wildbunch_runtime"] = "different-password"
        self.secrets.values["runtime"] = {
            "username": "wildbunch_runtime",
            "password": "persisted-password",
            "host": self.endpoint.host,
            "port": self.endpoint.port,
            "database": self.endpoint.database,
        }

        with self.assertRaisesRegex(
            InitializationNeedsReconciliation,
            "restore the matching secret or reconcile the role",
        ):
            initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual("different-password", self.admin.roles["wildbunch_runtime"])

    def test_secret_store_denial_is_not_treated_as_a_missing_secret(self) -> None:
        self.secrets.deny_reads = True

        with self.assertRaisesRegex(PermissionError, "secret access denied"):
            initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual([], self.secrets.create_calls)

    def test_aws_secret_adapter_populates_existing_secret_arn_without_creating_a_container(self) -> None:
        client = FakeSecretsManagerClient()
        value = {"username": "wildbunch_runtime", "password": "opaque"}
        store = BotoSecretStore(client, {"runtime": "arn:aws:secretsmanager:eu-north-1:123456789012:secret:runtime"})

        self.assertIsNone(store.read("runtime"))
        self.assertEqual(value, store.create_if_absent("runtime", value))

        self.assertEqual(
            [
                {
                    "SecretId": "arn:aws:secretsmanager:eu-north-1:123456789012:secret:runtime",
                    "SecretString": '{"username":"wildbunch_runtime","password":"opaque"}',
                }
            ],
            client.put_calls,
        )

    def test_aws_secret_adapter_reuses_an_existing_secret_version(self) -> None:
        client = FakeSecretsManagerClient()
        stored = {"username": "wildbunch_runtime", "password": "persisted"}
        client.values["runtime"] = '{"username":"wildbunch_runtime","password":"persisted"}'
        store = BotoSecretStore(client, {"runtime": "runtime"})

        self.assertEqual(stored, store.create_if_absent("runtime", {"password": "new"}))
        self.assertEqual([], client.put_calls)


if __name__ == "__main__":
    unittest.main()
