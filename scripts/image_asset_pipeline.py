#!/usr/bin/env python3
"""Compatibility wrapper for the asset-local image pipeline helper."""

from __future__ import annotations

import runpy
from pathlib import Path


TARGET = Path(__file__).resolve().parents[1] / "src" / "WildBunch.Assets" / "scripts" / "image_asset_pipeline.py"


if __name__ == "__main__":
    runpy.run_path(str(TARGET), run_name="__main__")
