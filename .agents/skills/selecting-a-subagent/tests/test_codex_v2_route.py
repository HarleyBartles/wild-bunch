from pathlib import Path


PROFILE = Path(__file__).resolve().parents[1] / "references" / "codex-multi-agent-v2-profile.md"


def test_codex_v2_uses_live_gpt6_routes_with_astra_ceiling() -> None:
    text = PROFILE.read_text(encoding="utf-8")
    assert "gpt-6-sol" in text
    assert "gpt-6-luna" in text
    assert "gpt-6-astra" in text
    assert "gpt-5.6-" not in text
    assert "Astra reasoning must never exceed `low`" in text
    assert "live runtime inventory" in text
