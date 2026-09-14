# Image Asset Cut and Normalization

Use this runbook after `/town-hub-asset-judgment` accepts a candidate that needs
deterministic processing.

## What belongs here

- cutout and normalization commands
- repository paths and canvas handling

## Required dependencies

- Python 3.11+ with Pillow installed for the primary pipeline path
- `src/WildBunch.Assets/scripts/image_asset_pipeline.py` depends on Pillow for the cut, slice, and normalize commands

## First-pass workflow

1. If the source is a full turnaround sheet, slice it into its individual
   views first using the repo helper.
2. Cut the background to transparency or normalize the crop onto a white
   staging canvas, depending on the review stage.
3. Normalize the subject onto the target canvas with a stable bottom anchor.
4. Write the result into the appropriate staging folder for visual judgment.
5. Promote it only after `/town-hub-asset-judgment` accepts the processed result.

## Cut and normalize command

Use the repository's primary Python backend first:

```bash
python src/WildBunch.Assets/scripts/image_asset_pipeline.py normalize \
  --input C:/path/to/source.png \
  --out path/to/staging/output.png
```

For full turnaround sheets, slice the views into separate staging files:

```bash
python src/WildBunch.Assets/scripts/image_asset_pipeline.py slice-sheet \
  --input C:/path/to/sheet.png \
  --out-dir path/to/staging/family \
  --names front,profile,rear,front-oblique,rear-oblique
```

If the environment does not have Pillow available, install it into the active
Python environment before using the repo helper.
