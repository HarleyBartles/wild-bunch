# Writing profile result contract

The evaluator returns UTF-8 JSON with stable key and finding order:

```json
{
  "schema_version": 1,
  "profile_id": "ai-prose-fatigue",
  "profile_version": "1.0.0",
  "input_sha256": "...",
  "status": "findings",
  "findings": [],
  "warnings": []
}
```

Each finding contains `type`, `pattern_id`, `evidence`, `span`, `rationale`,
`preserve_when`, `repair`, and `confidence`. `confidence` is always `null`:
deterministic rule matches do not justify a fabricated probability.

`type` is `observed`, `candidate`, `preserve`, `repair`, or `abstain`.
`span` contains zero-based `start` and exclusive `end` character offsets.
`status` is `findings`, `clear`, or `abstained`.

Results are observations about the supplied text and declared task context.
They are not authorship findings, detector scores, or instructions to evade a
classifier. The evaluator never edits the input.
