from pathlib import Path


SKILL = Path(__file__).resolve().parents[1] / "SKILL.md"


def test_creation_requires_bundled_script() -> None:
    text = SKILL.read_text(encoding="utf-8")
    assert "Use the bundled `new_worktree.py` script for every new worktree" in text
    assert "Native Worktree Tools (preferred)" not in text
    assert "git worktree add" not in text.split("## Step 1: Create Isolated Workspace", 1)[1]
