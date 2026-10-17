"""Build the three immutable local images for one clean source revision."""

from __future__ import annotations

import argparse
import re
import sys

from tools.deployment.process import CommandFailed, run_checked


_SOURCE_SHA = re.compile(r"^[0-9a-f]{40}$")
_IMAGES = (
    ("api", "deploy/containers/api.Dockerfile"),
    ("frontend", "deploy/containers/frontend.Dockerfile"),
    ("migrations", "deploy/containers/migrations.Dockerfile"),
)


def build(source_sha: str) -> None:
    if not _SOURCE_SHA.fullmatch(source_sha):
        raise ValueError("--source-sha must be a full 40-character lowercase Git SHA")

    checkout_sha = run_checked(["git", "rev-parse", "HEAD"]).strip()
    if source_sha != checkout_sha:
        raise ValueError("--source-sha does not match the current checkout")

    if run_checked(["git", "status", "--porcelain"]):
        raise ValueError("Image builds require a clean checkout")

    for image_name, dockerfile in _IMAGES:
        run_checked(
            [
                "docker",
                "buildx",
                "build",
                "--load",
                "--platform",
                "linux/amd64",
                "--tag",
                f"wild-bunch/{image_name}:{source_sha}",
                "--file",
                dockerfile,
                "--build-arg",
                f"WILD_BUNCH_RELEASE={source_sha}",
                ".",
            ]
        )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)
    build_parser = subparsers.add_parser("build", help="build all three production images")
    build_parser.add_argument("--source-sha", required=True)
    args = parser.parse_args(argv)

    try:
        build(args.source_sha)
    except (CommandFailed, ValueError) as error:
        parser.error(str(error))

    print(f"Built API, frontend and migration images for {args.source_sha}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
