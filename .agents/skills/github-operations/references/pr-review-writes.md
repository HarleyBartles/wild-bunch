# Native GitHub PR Review Writes

Use this reference only after `github-operations` has determined that the latest user request explicitly authorizes a GitHub PR review write with inline comments, review comment arrays, file/line comments, or same-token agent review feedback.

## Owned action

Submit a native GitHub pull request review containing inline review comments. This is different from adding a generic PR timeline comment or issue comment.

Use this route when the user wants reviewer-agent output to appear as GitHub review comments attached to diff lines, including cases where the PR author agent and reviewer agent share the same authenticated connector identity.

## Do not use this route for

- ordinary read-only PR verification;
- approval counting or branch-protection bypasses;
- generic progress notes, issue comments, or PR timeline comments;
- feedback evaluation after comments already exist;
- `APPROVE` or `REQUEST_CHANGES` unless the user explicitly asks for that state and GitHub permits it.

## Same-authored or same-token agent reviews

When the authoring agent and reviewing agent use the same GitHub authenticated actor, prefer a submitted review with `event: "COMMENT"`. Do not try to force `REQUEST_CHANGES` merely to make the review feel stronger. Same-token review identity is a provenance limitation; name the logical reviewer in the review body or each comment body when useful.

Native review comments are still useful even when they do not block merge or count as approval. They preserve file and line context and keep the feedback grouped as a PR review.

## Review payload contract

Create a pull request review against the specific repository and PR number with:

```json
{
  "commit_id": "<latest-pr-head-sha>",
  "event": "COMMENT",
  "body": "<optional grouped review summary>",
  "comments": [
    {
      "path": "<file path in the PR diff>",
      "line": 42,
      "side": "RIGHT",
      "body": "<inline review comment>"
    }
  ]
}
```

For multi-line comments, use the connector or API shape equivalent to `start_line`, `start_side`, `line`, and `side` when supported. Prefer `line`/`side` over deprecated positional forms. Use the latest PR head SHA for `commit_id` so comments attach to the current diff.

## Source checks before writing

Before submitting the review, verify or obtain:

1. repository owner/name;
2. PR number;
3. latest PR head SHA;
4. each target file path as it appears in the PR diff;
5. each target line is present on the intended side of the diff;
6. the latest user request authorizes this exact review write.

If the line cannot be mapped to the diff, do not silently downgrade the item to a timeline comment. Either omit the unmappable inline item with a clear note in the review body or report the mapping blocker and ask for a revised target, depending on the user's tolerance for partial review submission.

## Connector safety

Treat the review submission as a connector write. Keep one side effect per target review, avoid unrelated mutations, and preserve a factual failure report if the connector or API rejects the review. A blocked native review is not a completed review and not permission to pretend a generic comment is equivalent.

Safe fallback language:

```text
Native PR review submission was unavailable or blocked. I did not submit a fallback timeline comment because that would lose inline review context. The unmapped/native-review feedback is preserved below for manual submission.
```

Only submit a generic PR or issue comment as a fallback when the user explicitly authorizes that downgrade or previously specified that fallback behavior.
