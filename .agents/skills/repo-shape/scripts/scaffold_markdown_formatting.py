#!/usr/bin/env python3
"""Adopt, enforce, or validate the optional Markdown formatting surface."""

from __future__ import annotations

import argparse
import importlib.util
import json
import subprocess
import sys
from pathlib import Path


FORMATTER_VECTOR = [
    "@python",
    ".agents/skills/markdown-formatting/scripts/format_markdown.py",
]
GUIDANCE = (
    "## Markdown formatting\n\n"
    "When `.agents/contracts/markdown-formatting.json` is present, use the installed "
    "`markdown-formatting` skill command for repository-wide check/apply and for producer-scoped checks. "
    "Generated Markdown must be formatter-clean at its producer.\n"
)


def _root() -> Path:
    result = subprocess.run(["git", "rev-parse", "--show-toplevel"], text=True, capture_output=True, check=True)
    return Path(result.stdout.strip()).resolve()


def _formatter(root: Path):
    candidates = [
        root / ".agents/skills/markdown-formatting/scripts/format_markdown.py",
        Path(__file__).resolve().parents[2] / "markdown-formatting/scripts/format_markdown.py",
    ]
    path = next((candidate for candidate in candidates if candidate.is_file()), None)
    if path is None:
        raise RuntimeError("installed markdown-formatting skill is missing")
    spec = importlib.util.spec_from_file_location("markdown_formatter_contract", path)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module, path


def _vectors(value: object) -> list[list[str]]:
    if isinstance(value, list) and value and all(isinstance(item, str) for item in value):
        return [list(value)]
    if (
        isinstance(value, list)
        and value
        and all(isinstance(item, list) and item and all(isinstance(part, str) for part in item) for item in value)
    ):
        return [list(item) for item in value]
    raise RuntimeError("consumer command declaration has invalid command vectors")


def _check(root: Path) -> list[str]:
    findings: list[str] = []
    contract_path = root / ".agents/contracts/markdown-formatting.json"
    config_path = root / ".mdformat.toml"
    guidance_path = root / ".agents/playbooks/code-style.md"
    if not contract_path.is_file():
        return ["markdown formatting contract missing"]
    try:
        formatter, _ = _formatter(root)
        contract = formatter.load_contract(root)
        formatter.verify_configuration(root)
        formatter.verify_toolchain()
    except Exception as exc:
        findings.append(str(exc))
        return findings
    if not config_path.is_file():
        findings.append(".mdformat.toml missing")
    if not guidance_path.is_file() or "## Markdown formatting" not in guidance_path.read_text(encoding="utf-8"):
        findings.append("code-style guidance missing Markdown formatting section")
    declaration_path = root / ".agents/contracts/repo-standards-commands.json"
    if contract.state == "enforced":
        try:
            declaration = json.loads(declaration_path.read_text(encoding="utf-8"))
            apply = _vectors(declaration.get("apply"))
            check = _vectors(declaration.get("check"))
        except Exception as exc:
            findings.append(str(exc))
        else:
            if [*FORMATTER_VECTOR, "--apply"] not in apply:
                findings.append("enforced Markdown formatter apply command missing")
            if [*FORMATTER_VECTOR, "--check"] not in check:
                findings.append("enforced Markdown formatter check command missing")
    return findings


def _write_adopted(root: Path) -> None:
    templates = Path(__file__).resolve().parent.parent / "templates"
    contract = root / ".agents/contracts/markdown-formatting.json"
    config = root / ".mdformat.toml"
    guidance = root / ".agents/playbooks/code-style.md"
    contract.parent.mkdir(parents=True, exist_ok=True)
    guidance.parent.mkdir(parents=True, exist_ok=True)
    if not contract.exists():
        contract.write_bytes((templates / "markdown-formatting.json").read_bytes())
    if not config.exists():
        config.write_bytes((templates / "mdformat.toml").read_bytes())
    if guidance.exists():
        text = guidance.read_text(encoding="utf-8")
        if "## Markdown formatting" not in text:
            guidance.write_text(text.rstrip() + "\n\n" + GUIDANCE, encoding="utf-8", newline="\n")
    else:
        guidance.write_text("# Code Style\n\n" + GUIDANCE, encoding="utf-8", newline="\n")


def _enforce(root: Path) -> None:
    _write_adopted(root)
    formatter, formatter_path = _formatter(root)
    contract_path = root / ".agents/contracts/markdown-formatting.json"
    declaration_path = root / ".agents/contracts/repo-standards-commands.json"
    contract_before = contract_path.read_bytes()
    declaration_before = declaration_path.read_bytes()
    declaration = json.loads(declaration_path.read_text(encoding="utf-8"))
    apply = _vectors(declaration.get("apply"))
    check = _vectors(declaration.get("check"))
    contract_model = formatter.load_contract(root)
    formatted_files = formatter.eligible_markdown(root, contract_model)
    markdown_before = {path: path.read_bytes() for path in formatted_files}
    try:
        if formatter.main(["--apply"]):
            raise RuntimeError("Markdown formatting apply failed")
        if formatter.main(["--check"]):
            raise RuntimeError("Markdown formatting check failed after apply")
        contract = json.loads(contract_path.read_text(encoding="utf-8"))
        contract["state"] = "enforced"
        apply_vector = [*FORMATTER_VECTOR, "--apply"]
        check_vector = [*FORMATTER_VECTOR, "--check"]
        if apply_vector not in apply:
            apply.insert(0, apply_vector)
        if check_vector not in check:
            check.insert(0, check_vector)
        contract_path.write_text(json.dumps(contract, indent=2) + "\n", encoding="utf-8", newline="\n")
        declaration["apply"] = apply
        declaration["check"] = check
        declaration_path.write_text(json.dumps(declaration, indent=2) + "\n", encoding="utf-8", newline="\n")
    except BaseException:
        for path, content in markdown_before.items():
            path.write_bytes(content)
        contract_path.write_bytes(contract_before)
        declaration_path.write_bytes(declaration_before)
        raise


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Adopt or enforce Markdown formatting. (mixed)")
    parser.add_argument("--check", action="store_true", help="validate without mutation (read-only)")
    parser.add_argument("--apply", action="store_true", help="perform explicit adoption or enforcement")
    parser.add_argument("--state", choices=("adopted", "enforced"))
    args = parser.parse_args(argv)
    root = _root()
    if args.apply:
        if not args.state:
            parser.error("--apply requires --state")
        try:
            _write_adopted(root) if args.state == "adopted" else _enforce(root)
        except Exception as exc:
            print(f"ERROR: {exc}", file=sys.stderr)
            return 1
    findings = _check(root)
    if findings:
        for finding in findings:
            print(f"DRIFT: {finding}")
        return 1
    print("OK markdown-formatting surface")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
