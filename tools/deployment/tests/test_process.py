import sys
import unittest

from tools.deployment.process import CommandFailed, run_checked


class RunCheckedTests(unittest.TestCase):
    def test_nonzero_exit_does_not_expose_child_output_or_stdin(self) -> None:
        secret = "database-password-example"
        code = "import sys; print(sys.stdin.read()); print('secret-output-example', file=sys.stderr); sys.exit(7)"

        with self.assertRaises(CommandFailed) as failure:
            run_checked([sys.executable, "-c", code], stdin=secret)

        self.assertEqual(7, failure.exception.returncode)
        self.assertNotIn(secret, str(failure.exception))
        self.assertNotIn("secret-output-example", str(failure.exception))

    def test_success_returns_child_stdout(self) -> None:
        result = run_checked([sys.executable, "-c", "print('checked output')"])

        self.assertEqual("checked output\n", result)


if __name__ == "__main__":
    unittest.main()
