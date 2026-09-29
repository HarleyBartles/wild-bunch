from __future__ import annotations

from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from update_adr_freshness import check_readme, render_table, update_readme


def test_freshness_table_sorts_decisions_and_uses_latest_history_date(tmp_path: Path) -> None:
    readme = tmp_path / "README.md"
    readme.write_text("# Decisions\n", encoding="utf-8")
    (tmp_path / "ADR-0002-second.md").write_text(
        "## Status\n\n`superseded`\n\n## Dated Status History\n\n- 2026-02-01 - live\n- 2026-08-03 - superseded\n",
        encoding="utf-8",
    )
    (tmp_path / "ADR-0001-first.md").write_text(
        "## Status\n\n`live`\n\n## Dated Status History\n\n- 2026-04-19 - live\n",
        encoding="utf-8",
    )

    update_readme(readme)

    content = readme.read_text(encoding="utf-8")
    assert "[ADR-0001-first.md](ADR-0001-first.md) | live | 2026-04-19" in content
    assert "[ADR-0002-second.md](ADR-0002-second.md) | superseded | 2026-08-03" in content
    assert check_readme(readme) == []


def test_empty_decision_directory_removes_obsolete_table(tmp_path: Path) -> None:
    readme = tmp_path / "README.md"
    readme.write_text("# Decisions\n\n## Decision Status and Review Dates\n\nold table\n", encoding="utf-8")

    update_readme(readme)

    assert readme.read_text(encoding="utf-8") == "# Decisions\n"
    assert render_table(tmp_path) == ""
    assert check_readme(readme) == []
