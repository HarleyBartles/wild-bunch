# Product Roadmap and Milestone Scheme

This document defines the repo-level roadmap vocabulary for Wild Bunch.

The goal is to group work by broad product horizon without turning milestones into a second priority system or a date-driven release plan.

## Core Idea

- Labels answer urgency, timing, work type, and readiness.
- Milestones answer broad product or version horizon.
- Milestones are not a replacement for the existing `must` / `should` / `could` or `now` / `next` / `later` labels.
- Milestones are not a promise that a feature will ship on a date.

The roadmap should stay loose enough to keep long-running future work discoverable while leaving current-track work controlled by labels and the issue queue.

## Proposed Milestone Vocabulary

Use a small, stable set of horizon milestones:

- `Roadmap / Planning`
- `Core Loop`
- `v1`
- `v2 / Sandbox`
- `DLC / Future Packs`

These names describe product horizons, not calendar targets.

## How To Use Milestones

Assign a milestone when an issue clearly belongs to a broad product horizon and that horizon would help future readers understand why the issue exists.

Typical milestone-worthy cases:

- Core gameplay systems that must land before the first broadly playable version.
- Features that are clearly part of the first shippable version, even if they are not current-track work.
- Far-future ideas that are real enough to track, but should stay grouped as later product surfaces.
- Roadmap/process issues that define planning strategy rather than game content.

Leave an issue unmilestoned when:

- It is still too speculative to assign to a product horizon.
- It is a short-lived coordination task that does not describe product scope.
- The label axis already communicates the important state and a milestone would add noise.
- The issue is current-track work and should remain controlled by `now` / `next` / `later` plus `must` / `should` / `could`.

## Labels Versus Milestones

Use labels for the operational view:

- `must` / `should` / `could` describe importance.
- `now` / `next` / `later` describe timing.
- `feature` / `system` / `tooling` describe work type.
- `boring` describes the intended worker posture or slice style.

Use milestones for the product-horizon view:

- `Core Loop` means the work belongs to the earliest meaningful play loop.
- `v1` means the work belongs in the first broadly shippable product version.
- `v2 / Sandbox` means the work is later, larger, or exploratory but still product-shaping.
- `DLC / Future Packs` means the work is beyond the base release and belongs to future expansion space.
- `Roadmap / Planning` means the issue is about roadmap structure, milestone hygiene, or planning conventions themselves.

If a label and milestone seem to conflict, keep the label authoritative for urgency and readiness, and use the milestone only to show long-range product context.

## Example Mapping

These examples are illustrative only. They are not a queue reassignment.

- Core combat or travel loop work -> `Core Loop`
- First shippable town, case, or progression feature -> `v1`
- Cockpit sandbox or custom-adventure experimentation -> `v2 / Sandbox`
- Expansion-sized content pack or sequel-style system -> `DLC / Future Packs`
- This roadmap document or milestone policy work -> `Roadmap / Planning`

## Discoverability Rule

Future-facing issues should remain easy to find even when they are not current-track work.

Keep them discoverable by using:

- a clear issue title,
- the existing label taxonomy,
- an appropriate horizon milestone when one is genuinely known,
- and cross-links to related issues or planning notes.

If the horizon is not yet clear, leave the issue unmilestoned and let the labels carry the current state until the scope becomes stable enough for a milestone.

## Smallness Rule

This scheme is intentionally small.

Do not add new horizon milestones casually. If a new milestone seems necessary, first check whether the issue can fit into one of the existing horizons or remain unmilestoned.

Do not turn milestones into a shadow issue tracker. The queue, labels, and issue links stay the primary planning surface.
