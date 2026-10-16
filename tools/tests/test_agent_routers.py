from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_agent_routers

ROOT_ROUTER = (
    "# Project\n\n"
    "Read [the subscription](.agents/contracts/operating-standards.json) and "
    "[certification](.agents/contracts/standards-certification.md).\n\n"
    "## Guidance\n\n"
    "Read [testing](.agents/playbooks/testing.md) when changing behavior.\n"
)


def _root_fixture(tmp_path):
    (tmp_path / ".agents/contracts").mkdir(parents=True, exist_ok=True)
    (tmp_path / ".agents/contracts/operating-standards.json").write_text("{}", encoding="utf-8")
    (tmp_path / ".agents/contracts/standards-certification.md").write_text(
        "# Certifications", encoding="utf-8"
    )
    (tmp_path / ".agents/playbooks").mkdir(exist_ok=True)
    (tmp_path / ".agents/playbooks/testing.md").write_text("# Testing", encoding="utf-8")
    return {"AGENTS.md": ROOT_ROUTER}


def test_root_router_requires_subscription_and_certification_routes(tmp_path):
    assert check_agent_routers.check_routers(tmp_path, _root_fixture(tmp_path)) == []


def test_missing_subscription_route_fails(tmp_path):
    routers = _root_fixture(tmp_path)
    routers["AGENTS.md"] = ROOT_ROUTER.replace(
        "[the subscription](.agents/contracts/operating-standards.json) and ", ""
    )
    findings = check_agent_routers.check_routers(tmp_path, routers)
    assert any(
        "must route to .agents/contracts/operating-standards.json" in finding
        for finding in findings
    )


def test_broken_local_router_link_fails(tmp_path):
    routers = _root_fixture(tmp_path)
    routers["AGENTS.md"] = ROOT_ROUTER.replace(".agents/playbooks/testing.md", "missing.md")
    findings = check_agent_routers.check_routers(tmp_path, routers)
    assert any("missing local link target" in finding for finding in findings)


def test_root_budget_is_enforced(tmp_path):
    findings = check_agent_routers.check_routers(tmp_path, {"AGENTS.md": ROOT_ROUTER + "\n" * 40})
    assert any("exceeds 40-line budget" in finding for finding in findings)


def test_scoped_router_requires_one_scoped_read_condition(tmp_path):
    good = "When working in `src/Widget`, read [the local rules](rules.md).\n"
    (tmp_path / "src/Widget").mkdir(parents=True)
    (tmp_path / "src/Widget/rules.md").write_text("# Rules", encoding="utf-8")
    routers = _root_fixture(tmp_path)
    routers["src/Widget/AGENTS.md"] = good
    assert check_agent_routers.check_routers(tmp_path, routers) == []
    findings = check_agent_routers.check_routers(
        tmp_path,
        {
            **_root_fixture(tmp_path),
            "src/Widget/AGENTS.md": "Read the rules.\nThey apply everywhere.\n",
        },
    )
    assert any("one scoped sentence" in finding for finding in findings)
