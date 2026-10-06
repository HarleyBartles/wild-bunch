from __future__ import annotations

import json
import os
import time
import urllib.error
import urllib.request
import unittest
import uuid

import psycopg
from psycopg import sql

from deploy.database.init import (
    DatabaseEndpoint,
    PostgresAdmin,
    initialize_database,
)
from tools.deployment.process import run_checked


class MemorySecretStore:
    def __init__(self) -> None:
        self.values: dict[str, dict[str, object]] = {}

    def read(self, name: str) -> dict[str, object] | None:
        return self.values.get(name)

    def create_if_absent(self, name: str, value: dict[str, object]) -> dict[str, object]:
        return self.values.setdefault(name, value.copy())


def _admin_connection() -> PostgresAdmin:
    endpoint = DatabaseEndpoint(
        host=os.environ["WB_TEST_DB_HOST"],
        port=int(os.environ["WB_TEST_DB_PORT"]),
        sslmode="disable",
    )
    return PostgresAdmin(endpoint, os.environ["WB_TEST_DB_USERNAME"], os.environ["WB_TEST_DB_PASSWORD"])


@unittest.skipUnless(os.environ.get("WB_TEST_DB_PASSWORD"), "requires a disposable PostgreSQL container")
class PostgresRoleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.endpoint = DatabaseEndpoint(
            host=os.environ["WB_TEST_DB_HOST"],
            port=int(os.environ["WB_TEST_DB_PORT"]),
            sslmode="disable",
        )
        cls.admin = _admin_connection()
        cls.secrets = MemorySecretStore()
        initialize_database(cls.admin, cls.secrets, cls.endpoint)

    def test_a_runtime_cannot_create_alter_or_drop_schema_objects(self) -> None:
        suffix = uuid.uuid4().hex
        table_name = f"learning_privilege_{suffix}"
        sequence_name = f"learning_privilege_{suffix}_seq"
        migration_secret = self.secrets.values["migration"]
        runtime_secret = self.secrets.values["runtime"]

        with psycopg.connect(
            host=self.endpoint.host,
            port=self.endpoint.port,
            dbname=self.endpoint.database,
            user=str(migration_secret["username"]),
            password=str(migration_secret["password"]),
            sslmode="disable",
        ) as migration:
            with migration.cursor() as cursor:
                cursor.execute(
                    sql.SQL("CREATE TABLE public.{} (id bigint PRIMARY KEY, value text NOT NULL)").format(
                        sql.Identifier(table_name)
                    )
                )
                cursor.execute(sql.SQL("CREATE SEQUENCE public.{}").format(sql.Identifier(sequence_name)))

        with psycopg.connect(
            host=self.endpoint.host,
            port=self.endpoint.port,
            dbname=self.endpoint.database,
            user=str(runtime_secret["username"]),
            password=str(runtime_secret["password"]),
            sslmode="disable",
        ) as runtime:
            with runtime.cursor() as cursor:
                cursor.execute(
                    sql.SQL("INSERT INTO public.{} (id, value) VALUES (nextval(%s), %s)").format(
                        sql.Identifier(table_name)
                    ),
                    (f"public.{sequence_name}", "runtime can write"),
                )
                cursor.execute(sql.SQL("SELECT value FROM public.{}").format(sql.Identifier(table_name)))
                self.assertEqual(("runtime can write",), cursor.fetchone())

                ddl = (
                    sql.SQL("CREATE TABLE public.{} (id integer)").format(sql.Identifier(f"denied_{suffix}")),
                    sql.SQL("ALTER TABLE public.{} ADD COLUMN denied integer").format(sql.Identifier(table_name)),
                    sql.SQL("DROP TABLE public.{}").format(sql.Identifier(table_name)),
                )
                for statement in ddl:
                    with self.subTest(statement=statement.as_string(runtime)):
                        with self.assertRaises(psycopg.errors.InsufficientPrivilege):
                            cursor.execute(statement)
                        runtime.rollback()

        with psycopg.connect(
            host=self.endpoint.host,
            port=self.endpoint.port,
            dbname=self.endpoint.database,
            user=str(migration_secret["username"]),
            password=str(migration_secret["password"]),
            sslmode="disable",
        ) as migration:
            with migration.cursor() as cursor:
                cursor.execute(sql.SQL("DROP TABLE public.{}").format(sql.Identifier(table_name)))
                cursor.execute(sql.SQL("DROP SEQUENCE public.{}").format(sql.Identifier(sequence_name)))

    def test_runtime_role_has_only_expected_identity_authorities(self) -> None:
        with psycopg.connect(
            host=self.endpoint.host,
            port=self.endpoint.port,
            dbname="postgres",
            user=os.environ["WB_TEST_DB_USERNAME"],
            password=os.environ["WB_TEST_DB_PASSWORD"],
            sslmode="disable",
        ) as connection, connection.cursor() as cursor:
            for role in ("wildbunch_runtime", "wildbunch_migration"):
                cursor.execute(
                    "SELECT rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname = %s",
                    (role,),
                )
                self.assertEqual((False, False, False), cursor.fetchone())
            cursor.execute(
                "SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = %s",
                (self.endpoint.database,),
            )
            self.assertEqual("wildbunch_migration", cursor.fetchone()[0])

    def test_rerun_keeps_the_same_database_and_passwords(self) -> None:
        values = {name: value.copy() for name, value in self.secrets.values.items()}

        initialize_database(self.admin, self.secrets, self.endpoint)

        self.assertEqual(values, self.secrets.values)

    @unittest.skipUnless(
        os.environ.get("WB_TEST_MIGRATION_IMAGE") and os.environ.get("WB_TEST_API_IMAGE"),
        "requires the locally built migration and API images",
    )
    def test_z_migration_bundle_and_runtime_api_persist_a_game_setup(self) -> None:
        host_port = self.endpoint.port
        migration = self.secrets.values["migration"]
        runtime = self.secrets.values["runtime"]
        migration_connection = (
            f"Host=host.docker.internal;Port={host_port};Database={self.endpoint.database};"
            f"Username={migration['username']};Password={migration['password']};SSL Mode=Disable"
        )
        runtime_connection = (
            f"Host=host.docker.internal;Port={host_port};Database={self.endpoint.database};"
            f"Username={runtime['username']};Password={runtime['password']};SSL Mode=Disable"
        )
        docker_args = ["--add-host", "host.docker.internal:host-gateway"]

        run_checked(
            [
                "docker",
                "run",
                "--rm",
                *docker_args,
                "--env",
                "ConnectionStrings__WildBunchPostgresDb",
                "--env",
                "ASPNETCORE_ENVIRONMENT=Production",
                os.environ["WB_TEST_MIGRATION_IMAGE"],
            ],
            env={"ConnectionStrings__WildBunchPostgresDb": migration_connection},
        )

        container_name = f"wild-bunch-api-db-test-{uuid.uuid4().hex[:8]}"
        container_started = False
        try:
            run_checked(
                [
                    "docker",
                    "run",
                    "--detach",
                    "--name",
                    container_name,
                    *docker_args,
                    "--publish",
                    "127.0.0.1::8080",
                    "--env",
                    "ConnectionStrings__WildBunchPostgresDb",
                    "--env",
                    "ASPNETCORE_ENVIRONMENT=Production",
                    os.environ["WB_TEST_API_IMAGE"],
                ],
                env={"ConnectionStrings__WildBunchPostgresDb": runtime_connection},
            )
            container_started = True
            published = run_checked(["docker", "port", container_name, "8080/tcp"]).strip()
            base_url = f"http://{published}"
            for _ in range(30):
                if self._http_status(f"{base_url}/health/ready") == 200:
                    break
                time.sleep(1)
            else:
                self.fail("API did not become database-ready after the migration bundle")

            body = json.dumps({"playerName": "Learning Player"}).encode()
            self.assertEqual(201, self._http_status(f"{base_url}/api/games/setup", body))
        finally:
            if container_started:
                run_checked(["docker", "rm", "--force", container_name])

    @staticmethod
    def _http_status(url: str, body: bytes | None = None) -> int:
        request = urllib.request.Request(url, data=body, headers={"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(request, timeout=3) as response:
                return response.status
        except urllib.error.HTTPError as error:
            return error.code
        except urllib.error.URLError:
            return 0


if __name__ == "__main__":
    unittest.main()
