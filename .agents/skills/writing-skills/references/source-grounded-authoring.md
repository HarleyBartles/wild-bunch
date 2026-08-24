# Source-grounded authoring

Use `superpowers-plus:writing-skills` when creating, reviewing, or refreshing a
skill. Select the lane before drafting:

- `first_party` is original operational guidance with no authority assets.
- `skills-with-source` records one approved redistributable source in
  `assets/authority/reference-source/` and lists its decomposition.
- `skills-with-mixed-source` records multiple approved redistributable sources,
  each in a labelled subdirectory of `assets/authority/reference-source/`, and
  cites non-vendorable sources in `CITATIONS.md`.
- `skills-with-citation` is a clean-room synthesis from citable sources only and
  keeps no vendored source.

For any source-backed lane, decompose the authority into operational files under
`references/`. Every `authority.yaml` and `source-map.yaml` reference records its
`path`, `source_sections` mapping, `load_when` trigger, and `content_mode`.
`skills-with-mixed-source` prefixes `source_sections` with the source label when
a reference is derived from a vendored source (for example,
`postgresql: Server Administration`). Keep the two records reconciled: the
source map is the operational marketplace bundle of `authority.yaml`'s decomposition,
not a second authority manifest.

`skills-with-source` and `skills-with-mixed-source` require legal redistribution approval before a source is copied. Put approved cold material in
`assets/authority/reference-source/` and write operational prose from the
recorded decomposition. `skills-with-citation` must use `first_party_synthesis`
for every reference, keep no vendored source, and maintain scholarly evidence in
`assets/authority/CITATIONS.md`.

No inline citations belong in operational prose. Put authority metadata,
citations, derivation boundaries, reconciliation, and review evidence in
`assets/authority/`. Freshness is manual: a human performs a manual freshness
review, records retrieval details, and approves any refresh before the skill
changes.
