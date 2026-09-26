#!/usr/bin/env python3
"""Compatibility entrypoint for the repo-shape coordinator."""

from __future__ import annotations

import runpy
import sys
from pathlib import Path


TARGET = Path(__file__).resolve().parents[2] / "repo-shape" / "scripts" / "repo_standards.py"
sys.path.insert(0, str(TARGET.parent))
runpy.run_path(str(TARGET), run_name="__main__")
