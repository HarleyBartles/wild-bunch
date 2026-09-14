# Image asset cut and normalization runbook

## When

A town-hub candidate accepted by `/town-hub-asset-judgment` needs deterministic
sheet slicing, background removal, normalization, staging, or promotion.

## Required skills

- `/town-hub-asset-judgment`

## Composition

Accept visual judgment before processing, apply the deterministic operation,
then judge the processed game-scale result before promotion.

## Doctrine and contracts

The applicable art doctrine and `src/WildBunch.Assets/docs/asset-spec.md` bind
canvas, footprint, view names, and custody.

## Local commands and paths

Primary helper: `src/WildBunch.Assets/scripts/image_asset_pipeline.py` with
Python 3.11+ and Pillow.

```bash
python src/WildBunch.Assets/scripts/image_asset_pipeline.py normalize --input C:/path/to/source.png --out path/to/staging/output.png
python src/WildBunch.Assets/scripts/image_asset_pipeline.py slice-sheet --input C:/path/to/sheet.png --out-dir path/to/staging/family --names front,profile,rear,front-oblique,rear-oblique
```

## Evidence contract

The processed asset preserves required canvas, bottom anchor, transparency,
view naming, and accepted game-scale read at its staging or production path.

## Prohibited combinations

- Do not use deterministic cropping or perspective warping to rescue a rejected
  camera or family judgment.
