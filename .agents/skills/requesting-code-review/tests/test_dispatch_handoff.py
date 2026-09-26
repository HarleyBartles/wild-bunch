from pathlib import Path


SKILL = Path(__file__).resolve().parents[1] / "SKILL.md"


def test_dispatch_always_defers_route_to_selector() -> None:
    text = SKILL.read_text(encoding="utf-8")
    assert "selecting-a-subagent" in text.split("related_skills:", 1)[1].split("license:", 1)[0]
    assert "before every reviewer dispatch" in text.lower()
    assert "Dispatch a `general-purpose`" not in text
    assert "`reviewer-strong` for full branch" not in text
