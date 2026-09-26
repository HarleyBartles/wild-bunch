# Image asset cut and normalization playbook

## When

A town-hub candidate accepted by `/town-hub-asset-judgment` needs deterministic
sheet slicing, background removal, normalization, staging, or promotion.

## Required skills

- `/town-hub-asset-judgment`

## Composition

1. Use `/town-hub-asset-judgment` to accept the source family, view, and camera
   before deterministic processing.
2. Run the repo helper below to slice, cut, or normalize into the matching
   staging family without changing the accepted visual judgment.
3. Use `/town-hub-asset-judgment` again on the processed, game-scale result and
   promote only an accepted output to the asset-spec destination.

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

- [ ] The source and processed result both have recorded judgment.
- [ ] Canvas, bottom anchor, transparency, and view name match the asset spec.
- [ ] The promoted path belongs to the accepted family and output class.

## Prohibited combinations

- Do not use deterministic cropping or perspective warping to rescue a rejected
  camera or family judgment.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
