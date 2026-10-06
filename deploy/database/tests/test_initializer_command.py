"""Behavior tests for reading the RDS-managed administrator secret."""

from __future__ import annotations

import json
import unittest
from unittest.mock import Mock

from deploy.database.init import _admin_credentials


class AdminSecretReaderTests(unittest.TestCase):
    def test_reads_only_username_and_password_from_the_rds_secret(self) -> None:
        client = Mock()
        client.get_secret_value.return_value = {
            "SecretString": json.dumps(
                {
                    "username": "wild_bunch_admin",
                    "password": "not-for-logs",
                    "engine": "postgres",
                    "host": "database.internal",
                }
            )
        }

        self.assertEqual(_admin_credentials(client, "admin-secret-arn"), ("wild_bunch_admin", "not-for-logs"))
        client.get_secret_value.assert_called_once_with(SecretId="admin-secret-arn")

    def test_rejects_malformed_credentials_without_including_them_in_the_error(self) -> None:
        client = Mock()
        client.get_secret_value.return_value = {"SecretString": json.dumps({"username": "", "password": "credential-must-not-leak"})}

        with self.assertRaises(ValueError) as raised:
            _admin_credentials(client, "admin-secret-arn")

        self.assertNotIn("credential-must-not-leak", str(raised.exception))


if __name__ == "__main__":
    unittest.main()
