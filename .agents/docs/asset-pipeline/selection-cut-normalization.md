# Image Asset Selection, Cut, and Normalization

Use this note whenever generated art needs to be turned into shippable sprite
or image assets for the first time.

## What belongs here

- manual selection notes for candidate renders
- cutout and normalization commands
- promotion criteria from staging output to shippable output
- first-pass mistakes that should change the workflow next time

## Required dependencies

- Python 3.11+ with Pillow installed for the primary pipeline path
- `src/WildBunch.Assets/scripts/image_asset_pipeline.py` depends on Pillow for the cut, slice, and normalize commands

## First-pass workflow

1. Pick the candidate render that best matches the intended family and view.
2. If the source is a full turnaround sheet, slice it into its individual
   views first using the repo helper.
3. Cut the background to transparency or normalize the crop onto a white
   staging canvas, depending on the review stage.
4. Normalize the subject onto the target canvas with a stable bottom anchor.
5. Write the result into the appropriate staging folder until it has been reviewed.
6. Promote it only after the read, footprint, and framing are correct.

## Selection rules

- Prefer the render that best matches the intended view from the cleanest angle.
- Reject level-eye, painterly, or poster-like compositions when the asset should read as a sprite.
- If two candidates are close, pick the one that reads cleanly at game scale rather than the one with more decorative detail.
- Keep family-specific cues visible from every side that matters for the turnaround or placement contract.

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

## Follow-up

- Record any recurring failure mode here if it changes the prompt or
  normalization settings.
- If the output needs a new policy, update the human-facing spec and this
  note together.
