from __future__ import annotations

import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_operating_standards


def _repository(tmp_path, certification: str = ".agents/contracts/standards-certification.md#runbook-composition"):
    (tmp_path / "AGENTS.md").write_text(
        "Read [.agents/contracts/operating-standards.json](.agents/contracts/operating-standards.json) "
        "and [standards certification](.agents/contracts/standards-certification.md).\n",
        encoding="utf-8",
    )
    (tmp_path / ".agents/contracts").mkdir(parents=True)
    (tmp_path / ".agents/contracts/standards-certification.md").write_text(
        "# Certifications\n\n## runbook-composition\n\nAssessment.\n",
        encoding="utf-8",
    )
    record = {
        "version": 2,
        "standards": [
            {
                "id": "runbook-composition",
                "source": {
                    "repository": "https://github.com/HarleyBartles/agent-asset-marketplace.git",
                    "commit": "b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa",
                    "definition": "skills/runbook-composition/references/standard.md",
                },
                "certification": certification,
            }
        ],
    }
    (tmp_path / ".agents/contracts/operating-standards.json").write_text(
        json.dumps(record), encoding="utf-8"
    )
    return tmp_path


def test_valid_pin_and_existing_certification_section_pass(tmp_path):
    assert check_operating_standards.check_repository(_repository(tmp_path)) == []


def test_missing_certification_section_fails(tmp_path):
    findings = check_operating_standards.check_repository(
        _repository(tmp_path, ".agents/contracts/standards-certification.md#missing")
    )
    assert any("certification section is missing" in finding for finding in findings)


def test_malformed_immutable_pin_fails(tmp_path):
    root = _repository(tmp_path)
    path = root / ".agents/contracts/operating-standards.json"
    record = json.loads(path.read_text(encoding="utf-8"))
    record["standards"][0]["source"]["commit"] = "main"
    path.write_text(json.dumps(record), encoding="utf-8")
    assert any("full lowercase" in finding for finding in check_operating_standards.check_repository(root))


def test_legacy_record_is_rejected_as_explicit_migration_work(tmp_path):
    path = tmp_path / ".agents/contracts/operating-standards.json"
    path.parent.mkdir(parents=True)
    path.write_text('{"version":1,"standards":[]}', encoding="utf-8")
    assert "legacy version 1" in check_operating_standards.check_repository(tmp_path)[0]
