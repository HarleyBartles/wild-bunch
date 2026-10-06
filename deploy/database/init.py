"""Create the private application's database identities without rotating credentials."""

from __future__ import annotations

import json
import os
import secrets
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Protocol

import boto3
import psycopg
from psycopg import sql


DATABASE_NAME = "wildbunch"
MIGRATION_USERNAME = "wildbunch_migration"
RUNTIME_USERNAME = "wildbunch_runtime"
SECRET_FIELDS = frozenset({"username", "password", "host", "port", "database"})


class SecretStore(Protocol):
    def read(self, name: str) -> dict[str, object] | None: ...

    def create_if_absent(self, name: str, value: dict[str, object]) -> dict[str, object]: ...


class AdminConnection(Protocol):
    def role_exists(self, username: str) -> bool: ...

    def authenticate(self, username: str, password: str, database: str) -> None: ...

    def create_role(self, username: str, password: str) -> None: ...

    def database_exists(self, database: str) -> bool: ...

    def create_database(self, database: str, owner: str) -> None: ...

    def configure_privileges(self, database: str, migration_username: str, runtime_username: str) -> None: ...


@dataclass(frozen=True)
class DatabaseEndpoint:
    host: str
    port: int = 5432
    database: str = DATABASE_NAME
    sslmode: str = "verify-full"
    sslrootcert: str | None = None


class InitializationNeedsReconciliation(RuntimeError):
    """Existing identity and secret state disagree and must not be overwritten."""


class ExistingCredentialRejected(RuntimeError):
    """PostgreSQL explicitly rejected a previously stored identity credential."""


@dataclass(frozen=True)
class _Identity:
    secret_name: str
    username: str


_IDENTITIES = (
    _Identity("runtime", RUNTIME_USERNAME),
    _Identity("migration", MIGRATION_USERNAME),
)


def _credential_value(identity: _Identity, password: str, endpoint: DatabaseEndpoint) -> dict[str, object]:
    return {
        "username": identity.username,
        "password": password,
        "host": endpoint.host,
        "port": endpoint.port,
        "database": endpoint.database,
    }


def _validate_secret(
    identity: _Identity,
    secret: dict[str, object],
    endpoint: DatabaseEndpoint,
) -> dict[str, object]:
    if set(secret) != SECRET_FIELDS:
        raise InitializationNeedsReconciliation(
            f"The {identity.secret_name} secret shape is invalid; "
            "restore the expected fields or reconcile it manually."
        )
    expected = _credential_value(identity, str(secret.get("password", "")), endpoint)
    if secret != expected or not isinstance(secret.get("password"), str) or not secret["password"]:
        raise InitializationNeedsReconciliation(
            f"The {identity.secret_name} secret does not match this database endpoint; "
            "restore the matching secret or reconcile the role."
        )
    return secret


def initialize_database(
    admin_connection: AdminConnection,
    secret_store: SecretStore,
    endpoint: DatabaseEndpoint,
) -> None:
    """Initialize roles, database and grants; established credentials are never reset."""
    roles_exist = {identity.secret_name: admin_connection.role_exists(identity.username) for identity in _IDENTITIES}
    stored = {identity.secret_name: secret_store.read(identity.secret_name) for identity in _IDENTITIES}

    for identity in _IDENTITIES:
        if roles_exist[identity.secret_name] and stored[identity.secret_name] is None:
            raise InitializationNeedsReconciliation(
                f"The {identity.username} role exists without its {identity.secret_name} secret; "
                "restore the secret or reconcile the role."
            )

    credentials: dict[str, dict[str, object]] = {}
    for identity in _IDENTITIES:
        existing = stored[identity.secret_name]
        if existing is not None:
            credential = _validate_secret(identity, existing, endpoint)
            if roles_exist[identity.secret_name]:
                try:
                    admin_connection.authenticate(str(credential["username"]), str(credential["password"]), "postgres")
                except ExistingCredentialRejected:
                    raise InitializationNeedsReconciliation(
                        f"The persisted {identity.secret_name} credential was rejected; "
                        "restore the matching secret or reconcile the role."
                    ) from None
            credentials[identity.secret_name] = credential

    for identity in _IDENTITIES:
        if identity.secret_name in credentials:
            continue
        requested = _credential_value(identity, secrets.token_urlsafe(48), endpoint)
        established = secret_store.create_if_absent(identity.secret_name, requested)
        credentials[identity.secret_name] = _validate_secret(identity, established, endpoint)

    for identity in _IDENTITIES:
        if not roles_exist[identity.secret_name]:
            admin_connection.create_role(identity.username, str(credentials[identity.secret_name]["password"]))

    if not admin_connection.database_exists(endpoint.database):
        admin_connection.create_database(endpoint.database, MIGRATION_USERNAME)
    admin_connection.configure_privileges(endpoint.database, MIGRATION_USERNAME, RUNTIME_USERNAME)

    for identity in _IDENTITIES:
        try:
            admin_connection.authenticate(
                str(credentials[identity.secret_name]["username"]),
                str(credentials[identity.secret_name]["password"]),
                endpoint.database,
            )
        except ExistingCredentialRejected:
            raise InitializationNeedsReconciliation(
                f"The initialized {identity.secret_name} identity could not authenticate; "
                "reconcile its role and secret before retrying."
            ) from None


class BotoSecretStore:
    """Secrets Manager adapter using only the AWS SDK credential chain."""

    def __init__(self, client: Any, secret_ids: dict[str, str]) -> None:
        self._client = client
        self._secret_ids = secret_ids

    def read(self, name: str) -> dict[str, object] | None:
        try:
            result = self._client.get_secret_value(SecretId=self._secret_ids[name])
        except self._client.exceptions.ResourceNotFoundException:
            return None

        secret_string = result.get("SecretString")
        if not isinstance(secret_string, str):
            raise ValueError(f"The {name} secret is not stored as a JSON string.")
        value = json.loads(secret_string)
        if not isinstance(value, dict):
            raise ValueError(f"The {name} secret must contain a JSON object.")
        return value

    def create_if_absent(self, name: str, value: dict[str, object]) -> dict[str, object]:
        established = self.read(name)
        if established is not None:
            return established
        self._client.put_secret_value(
            SecretId=self._secret_ids[name],
            SecretString=json.dumps(value, separators=(",", ":")),
        )
        return value


class PostgresAdmin:
    """PostgreSQL adapter that uses composed identifiers and parameterized values."""

    def __init__(
        self,
        endpoint: DatabaseEndpoint,
        username: str,
        password: str,
        maintenance_database: str = "postgres",
    ) -> None:
        self._endpoint = endpoint
        self._username = username
        self._password = password
        self._maintenance_database = maintenance_database

    def _connect(
        self,
        database: str,
        username: str | None = None,
        password: str | None = None,
    ) -> psycopg.Connection[Any]:
        options: dict[str, object] = {
            "host": self._endpoint.host,
            "port": self._endpoint.port,
            "dbname": database,
            "user": username or self._username,
            "password": self._password if password is None else password,
            "connect_timeout": 5,
            "application_name": "wild-bunch-db-initializer",
            "sslmode": self._endpoint.sslmode,
        }
        if self._endpoint.sslrootcert is not None:
            options["sslrootcert"] = self._endpoint.sslrootcert
        return psycopg.connect(**options)

    def role_exists(self, username: str) -> bool:
        with self._connect(self._maintenance_database) as connection, connection.cursor() as cursor:
            cursor.execute("SELECT 1 FROM pg_roles WHERE rolname = %s", (username,))
            return cursor.fetchone() is not None

    def authenticate(self, username: str, password: str, database: str) -> None:
        try:
            with self._connect(database, username, password):
                return
        except psycopg.OperationalError as error:
            if error.sqlstate in {"28P01", "28000"}:
                raise ExistingCredentialRejected(username) from None
            raise

    def create_role(self, username: str, password: str) -> None:
        statement = sql.SQL(
            "CREATE ROLE {} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD {}"
        ).format(sql.Identifier(username), sql.Literal(password))
        with self._connect(self._maintenance_database) as connection:
            with connection.cursor() as cursor:
                cursor.execute(statement)

    def database_exists(self, database: str) -> bool:
        with self._connect(self._maintenance_database) as connection, connection.cursor() as cursor:
            cursor.execute("SELECT 1 FROM pg_database WHERE datname = %s", (database,))
            return cursor.fetchone() is not None

    def create_database(self, database: str, owner: str) -> None:
        statement = sql.SQL("CREATE DATABASE {} OWNER {}").format(sql.Identifier(database), sql.Identifier(owner))
        with self._connect(self._maintenance_database) as connection:
            connection.autocommit = True
            with connection.cursor() as cursor:
                cursor.execute(statement)

    def configure_privileges(self, database: str, migration_username: str, runtime_username: str) -> None:
        owner_statement = sql.SQL("ALTER DATABASE {} OWNER TO {}").format(
            sql.Identifier(database), sql.Identifier(migration_username)
        )
        with self._connect(self._maintenance_database) as maintenance:
            maintenance.autocommit = True
            with maintenance.cursor() as cursor:
                cursor.execute(owner_statement)

        with self._connect(database) as connection, connection.cursor() as cursor:
            cursor.execute(
                sql.SQL("ALTER SCHEMA public OWNER TO {}").format(sql.Identifier(migration_username))
            )
            privilege_script = Path(__file__).with_name("roles.sql").read_text(encoding="utf-8")
            for statement in privilege_script.split(";"):
                if statement.strip():
                    cursor.execute(statement)

            cursor.execute("SELECT CURRENT_USER")
            admin_username = cursor.fetchone()[0]
            cursor.execute(
                sql.SQL("GRANT {} TO {}").format(
                    sql.Identifier(migration_username), sql.Identifier(admin_username)
                )
            )
            try:
                cursor.execute(sql.SQL("SET ROLE {}").format(sql.Identifier(migration_username)))
                cursor.execute(
                    "ALTER DEFAULT PRIVILEGES IN SCHEMA public "
                    "GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO wildbunch_runtime"
                )
                cursor.execute(
                    "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO wildbunch_runtime"
                )
            finally:
                cursor.execute("RESET ROLE")
                cursor.execute(
                    sql.SQL("REVOKE {} FROM {}").format(
                        sql.Identifier(migration_username), sql.Identifier(admin_username)
                    )
                )


def _required_environment(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise ValueError(f"Required environment variable {name} is missing.")
    return value


def main() -> int:
    try:
        endpoint = DatabaseEndpoint(
            host=_required_environment("WB_DB_HOST"),
            port=int(os.environ.get("WB_DB_PORT", "5432")),
            sslmode=os.environ.get("WB_DB_SSLMODE", "verify-full"),
            sslrootcert=os.environ.get("WB_DB_SSLROOTCERT"),
        )
        admin = PostgresAdmin(
            endpoint,
            username=_required_environment("WB_DB_ADMIN_USERNAME"),
            password=_required_environment("WB_DB_ADMIN_PASSWORD"),
        )
        secret_ids = {
            "runtime": _required_environment("WB_RUNTIME_SECRET_ID"),
            "migration": _required_environment("WB_MIGRATION_SECRET_ID"),
        }
        store = BotoSecretStore(boto3.client("secretsmanager"), secret_ids)
        initialize_database(admin, store, endpoint)
    except InitializationNeedsReconciliation as error:
        print(str(error), file=sys.stderr)
        return 2
    except Exception as error:
        print(f"Database initialization failed ({type(error).__name__}).", file=sys.stderr)
        return 1

    print("Database roles and grants are initialized.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
