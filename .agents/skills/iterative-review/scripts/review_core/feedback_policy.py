#!/usr/bin/env python3
"""Feedback-history policy and GitHub enumeration (Plan 2 Task 4).

The actionable feedback set is every reviewThread (resolved or not) plus
every review in state CHANGES_REQUESTED. Enumeration fails closed on
graphql errors, transport failure, or any paged collection (v1 refuses to
silently truncate rather than pretending completeness).
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass

from . import model

FEEDBACK_POLICY_ID = "feedback-history"
FEEDBACK_POLICY_VERSION = "1"

_FEEDBACK_DOC = {
    "policy_id": FEEDBACK_POLICY_ID,
    "version": FEEDBACK_POLICY_VERSION,
    "actionable": {
        "threads": "all",
        "reviews": ["CHANGES_REQUESTED"],
    },
    "severity": {
        "review:CHANGES_REQUESTED": "blocking",
        "thread:unresolved": "important",
        "thread:resolved": "minor",
    },
}

_GRAPHQL = """
query($owner:String!, $repo:String!, $number:Int!) {
  repository(owner:$owner, name:$repo) {
    pullRequest(number:$number) {
      reviewThreads(first:100) {
        pageInfo { hasNextPage }
        nodes {
          id isResolved isOutdated path line
          comments(first:50) {
            pageInfo { hasNextPage }
            nodes { body author { login } createdAt }
          }
        }
      }
      reviews(first:100) {
        pageInfo { hasNextPage }
        nodes { id state body author { login } submittedAt }
      }
    }
  }
}
"""

_PR_URL_RE = re.compile(r"^https://github\.com/([^/]+)/([^/]+)/pull/(\d+)$")


class FeedbackPolicyError(Exception):
    """Feedback enumeration failed closed."""


@dataclass(frozen=True)
class FeedbackItem:
    canonical_id: str
    provider: str
    thread_id: str
    resolution_state: str
    bytes_sha256: str
    raw: bytes


@dataclass(frozen=True)
class FeedbackHistoryPolicy:
    policy_id: str
    version: str
    document_sha256: str


def default_policy() -> FeedbackHistoryPolicy:
    return FeedbackHistoryPolicy(
        policy_id=FEEDBACK_POLICY_ID,
        version=FEEDBACK_POLICY_VERSION,
        document_sha256=model.sha256_hex(model.canonical_json(_FEEDBACK_DOC)),
    )


def parse_pr_url(pr_url: str) -> tuple[str, str, int]:
    m = _PR_URL_RE.match(pr_url.strip())
    if not m:
        raise FeedbackPolicyError(f"unsupported pr_url {pr_url!r}")
    return m.group(1), m.group(2), int(m.group(3))


def _check_page(info: object, what: str) -> None:
    if not isinstance(info, dict) or info.get("hasNextPage") is not False:
        raise FeedbackPolicyError(f"{what} pageInfo missing or hasNextPage: refusing truncation")


def enumerate_feedback(*, run_gh, pr_url: str) -> list[FeedbackItem]:
    owner, repo, number = parse_pr_url(pr_url)
    rc, out, err = run_gh(
        [
            "api",
            "graphql",
            "-f",
            f"query={_GRAPHQL}",
            "-F",
            f"owner={owner}",
            "-F",
            f"repo={repo}",
            "-F",
            f"number={number}",
        ]
    )
    if rc != 0:
        raise FeedbackPolicyError(f"gh api graphql failed: {err.strip()}")
    try:
        doc = json.loads(out)
    except Exception as exc:
        raise FeedbackPolicyError(f"graphql response not JSON: {exc}") from exc
    if not isinstance(doc, dict):
        raise FeedbackPolicyError("graphql response is not an object")
    if doc.get("errors"):
        raise FeedbackPolicyError(f"graphql errors: {doc['errors']}")
    try:
        pr = doc["data"]["repository"]["pullRequest"]
        threads = pr["reviewThreads"]
        reviews = pr["reviews"]
    except (TypeError, KeyError) as exc:
        raise FeedbackPolicyError(f"graphql projection missing: {exc}") from exc
    if not isinstance(threads, dict) or not isinstance(reviews, dict):
        raise FeedbackPolicyError("graphql projection malformed: reviewThreads/reviews not objects")
    _check_page(threads.get("pageInfo"), "reviewThreads")
    _check_page(reviews.get("pageInfo"), "reviews")
    for coll, what in ((threads, "reviewThreads"), (reviews, "reviews")):
        if not isinstance(coll.get("nodes"), list):
            raise FeedbackPolicyError(f"graphql projection malformed: {what}.nodes not a list")

    items: list[FeedbackItem] = []
    for node in threads["nodes"]:
        if not isinstance(node, dict) or not isinstance(node.get("id"), str):
            raise FeedbackPolicyError("graphql projection malformed: reviewThreads node")
        comments = node.get("comments")
        if not isinstance(comments, dict):
            raise FeedbackPolicyError("graphql projection malformed: thread.comments")
        _check_page(comments.get("pageInfo"), "thread.comments")
        raw = model.canonical_json({"kind": "thread", "node": node})
        items.append(
            FeedbackItem(
                canonical_id=f"github:thread:{node['id']}",
                provider="github",
                thread_id=node["id"],
                resolution_state="resolved" if node.get("isResolved") else "unresolved",
                bytes_sha256=model.sha256_hex(raw),
                raw=raw,
            )
        )
    for node in reviews["nodes"]:
        if not isinstance(node, dict):
            raise FeedbackPolicyError("graphql projection malformed: reviews node")
        if node.get("state") != "CHANGES_REQUESTED":
            continue
        if not isinstance(node.get("id"), str):
            raise FeedbackPolicyError("graphql projection malformed: review node id")
        raw = model.canonical_json({"kind": "review", "node": node})
        items.append(
            FeedbackItem(
                canonical_id=f"github:review:{node['id']}",
                provider="github",
                thread_id=node["id"],
                resolution_state="unresolved",
                bytes_sha256=model.sha256_hex(raw),
                raw=raw,
            )
        )
    return items


def _canonical_items(items) -> list[dict]:
    return sorted(
        (
            {
                "canonical_id": i.canonical_id,
                "provider": i.provider,
                "thread_id": i.thread_id,
                "resolution_state": i.resolution_state,
                "bytes_sha256": i.bytes_sha256,
            }
            for i in items
        ),
        key=lambda r: r["canonical_id"],
    )


def item_from_raw(raw: bytes) -> FeedbackItem:
    try:
        doc = json.loads(raw)
        kind = doc["kind"]
        node = doc["node"]
        node_id = node["id"]
    except Exception as exc:
        raise FeedbackPolicyError(f"feedback evidence not a canonical item: {exc}") from exc
    if kind == "thread":
        return FeedbackItem(
            canonical_id=f"github:thread:{node_id}",
            provider="github",
            thread_id=node_id,
            resolution_state="resolved" if node.get("isResolved") else "unresolved",
            bytes_sha256=model.sha256_hex(raw),
            raw=raw,
        )
    if kind == "review":
        return FeedbackItem(
            canonical_id=f"github:review:{node_id}",
            provider="github",
            thread_id=node_id,
            resolution_state="unresolved",
            bytes_sha256=model.sha256_hex(raw),
            raw=raw,
        )
    raise FeedbackPolicyError(f"feedback evidence has unknown kind {kind!r}")


def feedback_history_sha256(items) -> str:
    return model.sha256_json(_canonical_items(items))


def unresolved_feedback_sha256(items) -> str:
    return model.sha256_json([r for r in _canonical_items(items) if r["resolution_state"] == "unresolved"])


def _severity(item: FeedbackItem) -> str:
    if item.canonical_id.startswith("github:review:"):
        return "blocking"
    return "important" if item.resolution_state == "unresolved" else "minor"


def _title(item: FeedbackItem) -> str:
    try:
        node = json.loads(item.raw)["node"]
    except Exception:
        node = {}
    if item.canonical_id.startswith("github:review:"):
        body = (node.get("body") or "").strip().splitlines()
        head = body[0][:72] if body else "changes requested"
        author = (node.get("author") or {}).get("login", "reviewer")
        return f"Review feedback from {author}: {head}"
    comments = (node.get("comments") or {}).get("nodes") or []
    first = comments[0] if isinstance(comments, list) and comments and isinstance(comments[0], dict) else {}
    body = first.get("body") if isinstance(first.get("body"), str) else ""
    lines = body.strip().splitlines() or ["review thread"]
    path = node.get("path") or ""
    line = node.get("line")
    locus = f"{path}:{line}" if path else "thread"
    return f"Review thread on {locus}: {lines[0][:64]}"


def feedback_findings(items, *, policy: FeedbackHistoryPolicy) -> list[dict]:
    findings = []
    for item in items:
        findings.append(
            {
                "source_kind": "feedback",
                "source_id": item.canonical_id,
                "source_assignment_id": item.canonical_id,
                "obligation_id": None,
                "severity": _severity(item),
                "title": _title(item),
                "description": item.raw.decode("utf-8", errors="replace"),
                "locations": [item.canonical_id],
                "evidence_ids": [],
                "regression_of": None,
                "disposition": "open",
                "resolution": None,
            }
        )
    return findings
