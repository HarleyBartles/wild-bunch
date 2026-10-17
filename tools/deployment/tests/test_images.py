import subprocess
import sys
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]


class ImageBuildTests(unittest.TestCase):
    def test_rejects_image_source_sha_that_does_not_match_checkout(self) -> None:
        wrong_sha = "f" * 40
        result = subprocess.run(
            [sys.executable, "-m", "tools.deployment.images", "build", "--source-sha", wrong_sha],
            cwd=REPOSITORY_ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(2, result.returncode)
        self.assertIn("does not match the current checkout", result.stderr)


if __name__ == "__main__":
    unittest.main()
