---
name: town-hub-asset-judgment
description: Use when selecting, retrying, or promoting a generated Wild Bunch town-hub building, road, or ground asset.
metadata:
  status: active
  scope: Wild Bunch town-hub asset acceptance judgment.
  use_when:
    - Use when a generated town-hub asset needs family, camera, seam, retry, or promotion judgment.
  do_not_use_when:
    - Do not use for deterministic cutting or normalization mechanics alone.
---

# Town Hub Asset Judgment

Before using this skill, read the applicable [play-surface UI](../../unslop/play-surface-ui.md) for player presentation and [writing](../../unslop/writing.md) for briefs guards in full. Follow [selection and observations](../../unslop/README.md) when scope changes or a distinct incident warrants recording.

## Owned decision

Decide whether a candidate belongs to its intended family, satisfies its camera
or seam contract, needs a targeted retry, or is ready for staging/promotion.

## Method

1. Read the applicable human-facing asset bible and relevant doctrine under
   [art doctrine](../../doctrine/art/), including the building and ground
   guidance that applies to the candidate.
2. Compare the candidate at source scale and game scale against family,
   camera/turnaround, prosperity, footprint, transparency, and seam constraints.
3. Return `accept`, `retry`, or `reject`, with the failed constraint and the
   smallest corrective prompt or deterministic pipeline action.
4. Permit promotion only when the required views and scale checks agree.

## Boundary

This skill owns visual acceptance judgment. The town-hub asset production
runbook owns generation, deterministic processing, paths, and evidence.
