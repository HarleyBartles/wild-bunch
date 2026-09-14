#!/usr/bin/env python3
"""Tests for live acquisition + feedback-history policy (Plan 2 Task 4)."""

from __future__ import annotations

import json
import shutil
import sys
from pathlib import Path
from types import SimpleNamespace

import pytest

TESTS_DIR = Path(__file__).resolve().parent
SCRIPTS = TESTS_DIR.parent / "scripts"
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(SCRIPTS))

from review_core import model, policy, witness_log  # noqa: E402
from review_core import acquisition as acq  # noqa: E402
from review_core import feedback_policy as fbp  # noqa: E402
import review_v2_helpers as helpers  # noqa: E402
from review_v2_helpers import FakeGh, FakeGit  # noqa: E402

BASE = helpers.ACQ_BASE
HEAD = helpers.ACQ_HEAD
TREE = helpers.ACQ_TREE
MB = helpers.ACQ_MB
REPO_ID = helpers.ACQ_REPO_ID
PR_URL = helpers.ACQ_PR_URL

_pr_meta = helpers.acq_pr_meta
_scratch = helpers.acq_scratch
_enumerate = helpers.acq_enumerate
_transcript_with_marker = helpers.acq_transcript_with_marker
_source = helpers.acq_source


class TestEnumerateAcquisition:
    def test_freeze_payload_full_identity(self, tmp_path):
        summary, out_dir, _s = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_text())
        snap = data["snapshot"]
        assert snap["epoch"] == 1
        assert snap["repository_id"] == REPO_ID
        assert snap["pr_number"] == 7 and snap["pr_url"] == PR_URL
        assert snap["git_object_format"] == "sha1"
        assert snap["base_sha"] == BASE and snap["head_sha"] == HEAD
        assert snap["tree_sha"] == TREE
        assert snap["diff_sha256"] == model.sha256_hex(b"diff-bytes")
        for key in model.SNAPSHOT_SUBJECT_FIELDS:
            assert key in snap, key
        assert "fingerprint" not in snap
        assert summary["enumeration_id"] == model.sha256_json(
            model.authority_discovery_subject(
                {**snap, "fingerprint": model.snapshot_fingerprint(snap)}, data["manifest_payload"]
            )
        )

    def test_blocks_on_missing_gh_auth(self, tmp_path):
        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, gh=FakeGh(authed=False))
        assert ei.value.blocker_class == "tool-blocked"

    def test_blocks_on_ambiguous_merge_base(self, tmp_path):
        git = FakeGit({"AGENTS.md": "x"}, merge_bases=[MB, "f" * 40])
        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, git=git)
        assert ei.value.blocker_class == "snapshot-drift"

    def test_blocks_on_shallow_repo(self, tmp_path):
        git = FakeGit({"AGENTS.md": "x"})

        def git_shallow(a):
            if a[:2] == ["rev-parse", "--is-shallow-repository"]:
                return 0, "true\n", ""
            return git(a)

        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, git=git_shallow)
        assert ei.value.blocker_class == "snapshot-drift"

    def test_blocks_on_dirty_worktree(self, tmp_path):
        git = FakeGit({"AGENTS.md": "x"}, porcelain=" M f.py\n")
        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, git=git)
        assert ei.value.blocker_class == "snapshot-drift"

    def test_blocks_on_non_remote_head(self, tmp_path):
        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, gh=FakeGh(head_remote=False))
        assert ei.value.blocker_class == "snapshot-drift"

    def test_blocks_on_head_mismatch(self, tmp_path):
        git = FakeGit({"AGENTS.md": "x"}, head="0" * 40)
        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, git=git)
        assert ei.value.blocker_class == "snapshot-drift"

    def test_required_authority_unavailable_blocks(self, tmp_path):
        # AGENTS.md in ls-tree but unreadable at base -> authority-missing
        git = FakeGit({"AGENTS.md": "x"})
        orig = git.__call__

        def flaky(args):
            if args[0] == "show" and args[1].endswith(":AGENTS.md"):
                return 1, "", "corrupt object"
            return orig(args)

        with pytest.raises(acq.AcquisitionError) as ei:
            _enumerate(tmp_path, git=flaky)
        assert ei.value.blocker_class == "authority-missing"

    def test_optional_authority_degrades_to_unavailable(self, tmp_path):
        gh = FakeGh(issues={})  # issue 12 fetch 404s
        summary, out_dir, _s = _enumerate(tmp_path, gh=gh)
        data = json.loads((out_dir / "data.json").read_text())
        unavailable = [a for a in data["manifest_payload"]["authorities"] if a["availability"] == "unavailable"]
        assert unavailable and unavailable[0]["failure_sha256"]
        assert unavailable[0]["kind"] == "issue"

    def test_feedback_resolved_before_freeze_still_enumerated(self, tmp_path):
        threads = [
            {
                "id": "PRRT_1",
                "isResolved": True,
                "isOutdated": False,
                "path": "a.py",
                "line": 3,
                "comments": {
                    "pageInfo": {"hasNextPage": False},
                    "nodes": [{"body": "nit", "author": {"login": "rev"}, "createdAt": "2026-01-01T00:00:00Z"}],
                },
            }
        ]
        _sum, out_dir, _s = _enumerate(tmp_path, gh=FakeGh(threads=threads))
        data = json.loads((out_dir / "data.json").read_text())
        fb = [a for a in data["manifest_payload"]["authorities"] if a["kind"] == "review-feedback"]
        assert len(fb) == 1 and fb[0]["availability"] == "loaded"
        findings = data["findings"]
        assert any(f["source_kind"] == "feedback" and f["disposition"] == "open" for f in findings)

    def test_pr_metadata_projection_excludes_lifecycle(self, tmp_path):
        _s1, out1, _ = _enumerate(tmp_path / "a", gh=FakeGh(pr=_pr_meta(isDraft=True)))
        _s2, out2, _ = _enumerate(tmp_path / "b", gh=FakeGh(pr=_pr_meta(isDraft=False)))
        d1 = json.loads((out1 / "data.json").read_text())
        d2 = json.loads((out2 / "data.json").read_text())
        assert d1["snapshot"]["pr_metadata_sha256"] == d2["snapshot"]["pr_metadata_sha256"]

    def test_manifest_payload_field_contract(self, tmp_path):
        _s, out_dir, _ = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_text())
        payload = data["manifest_payload"]
        assert set(payload) == set(model.MANIFEST_PAYLOAD_FIELDS)
        assert "authority_manifest_id" not in payload

    def test_gh_doc_locator_path_is_url_encoded(self):
        gh = FakeGh(contents={"dir%20a/file.md": "remote doc"})
        seed = SimpleNamespace(locator="gh:doc/dir a/file.md")
        raw = acq._load_seed_bytes(
            seed,
            run_git=lambda a: (1, "", "unused"),
            run_gh=gh,
            base_sha=BASE,
            repo_id=REPO_ID,
            pr_meta=_pr_meta(),
        )
        assert raw == b"remote doc"
        api = [c for c in gh.calls if c[:1] == ["api"] and "/contents/" in c[1]]
        assert api and "dir%20a/file.md" in api[0][1]

    def test_gh_pr_locator_must_match_enumerated_pr(self):
        foreign = SimpleNamespace(locator="gh:pr/999#body")
        with pytest.raises(acq.AcquisitionError) as ei:
            acq._load_seed_bytes(
                foreign,
                run_git=lambda a: (1, "", "unused"),
                run_gh=FakeGh(),
                base_sha=BASE,
                repo_id=REPO_ID,
                pr_meta=_pr_meta(),
            )
        assert ei.value.blocker_class == "authority-missing"
        own = SimpleNamespace(locator="gh:pr/7#body")
        assert (
            acq._load_seed_bytes(
                own,
                run_git=lambda a: (1, "", "unused"),
                run_gh=FakeGh(),
                base_sha=BASE,
                repo_id=REPO_ID,
                pr_meta=_pr_meta(body="pr body text"),
            )
            == b"pr body text"
        )


class TestEnumerateFeedback:
    def test_threads_and_changes_requested_enumerated(self):
        threads = [
            {
                "id": "T1",
                "isResolved": False,
                "isOutdated": False,
                "path": "a.py",
                "line": 1,
                "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
            }
        ]
        reviews = [
            {"id": "R1", "state": "CHANGES_REQUESTED", "body": "fix", "author": {"login": "x"}, "submittedAt": "t"}
        ]
        items = fbp.enumerate_feedback(run_gh=FakeGh(threads=threads, reviews=reviews), pr_url=PR_URL)
        ids = {i.canonical_id for i in items}
        assert ids == {"github:thread:T1", "github:review:R1"}

    def test_next_page_fails_closed(self):
        gh = FakeGh()
        orig = gh.__call__

        def paged(args):
            rc, out, err = orig(args)
            if args[0] == "api" and args[1] == "graphql":
                obj = json.loads(out)
                obj["data"]["repository"]["pullRequest"]["reviewThreads"]["pageInfo"]["hasNextPage"] = True
                return rc, json.dumps(obj), err
            return rc, out, err

        with pytest.raises(fbp.FeedbackPolicyError):
            fbp.enumerate_feedback(run_gh=paged, pr_url=PR_URL)

    def test_graphql_errors_fail_closed(self):
        with pytest.raises(fbp.FeedbackPolicyError):
            fbp.enumerate_feedback(run_gh=FakeGh(graphql_error=True), pr_url=PR_URL)

    def test_malformed_projection_shapes_fail_closed(self):
        # Parseable-but-wrong graphql shapes must raise FeedbackPolicyError
        # (-> authority-missing), never AttributeError -> unexpected.
        mutations = [
            lambda pr: pr.update(reviewThreads=None),
            lambda pr: pr.update(reviewThreads=[]),
            lambda pr: pr.update(reviewThreads={"pageInfo": {"hasNextPage": False}}),
            lambda pr: pr.update(reviewThreads={"pageInfo": {"hasNextPage": False}, "nodes": [None]}),
            lambda pr: pr.update(reviewThreads={"pageInfo": {"hasNextPage": False}, "nodes": [{"comments": {}}]}),
            lambda pr: pr.update(
                reviews={"pageInfo": {"hasNextPage": False}, "nodes": [{"state": "CHANGES_REQUESTED"}]}
            ),
        ]
        for mutate in mutations:
            gh = FakeGh()
            orig = gh.__call__

            def wrapped(args, orig=orig, mutate=mutate):
                rc, out, err = orig(args)
                if args[:2] == ["api", "graphql"]:
                    obj = json.loads(out)
                    mutate(obj["data"]["repository"]["pullRequest"])
                    return rc, json.dumps(obj), err
                return rc, out, err

            with pytest.raises(fbp.FeedbackPolicyError):
                fbp.enumerate_feedback(run_gh=wrapped, pr_url=PR_URL)

    def test_severity_table(self):
        items = [
            fbp.FeedbackItem("github:review:R1", "github", "R1", "unresolved", "x" * 64, b"r"),
            fbp.FeedbackItem("github:thread:T1", "github", "T1", "unresolved", "y" * 64, b"t"),
            fbp.FeedbackItem("github:thread:T2", "github", "T2", "resolved", "z" * 64, b"t2"),
        ]
        findings = fbp.feedback_findings(items, policy=fbp.default_policy())
        sev = {f["source_id"]: f["severity"] for f in findings}
        assert sev["github:review:R1"] == "blocking"
        assert sev["github:thread:T1"] == "important"
        assert sev["github:thread:T2"] == "minor"

    def test_history_digests_split_unresolved(self):
        items = [
            fbp.FeedbackItem("github:thread:T1", "github", "T1", "unresolved", "y" * 64, b"t"),
            fbp.FeedbackItem("github:thread:T2", "github", "T2", "resolved", "z" * 64, b"t2"),
        ]
        assert fbp.feedback_history_sha256(items) != fbp.unresolved_feedback_sha256(items)


class TestLoadAcquisition:
    def test_acquire_freeze_builds_witness_and_payload(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        src = _source(out_dir, scratch)
        payload = src.acquire(action="freeze-review-input", current_snapshot=None)
        env = json.loads(payload.raw_data)
        # The envelope separates witnesses from data; the engine folds them
        # into the payload key set at complete time.
        assert set(env["data"]) == set(policy.ACTION_PAYLOAD_KEYS["freeze-review-input"]) - {"witnesses"}
        witnesses = env["witnesses"]
        assert len(witnesses) == 1
        rec = witnesses[0]
        assert rec["kind"] == "authority-discovery"
        assert rec["record_positions"] and rec["chain_head_at_record"]
        assert rec["transcript_range"]["session_id"] == "s1"
        # verify with the Task-1 verifier
        verifier = witness_log.TranscriptWitnessVerifier(
            witness_log.TranscriptWitnessPolicy(
                transcript_root=scratch / "transcripts", witness_root=scratch / "witness"
            ),
            witness_root=scratch / "witness",
            review_id="rev-1",
        )
        snap = env["data"]["snapshot"]
        data = json.loads((out_dir / "data.json").read_text())
        subject = model.authority_discovery_subject(snap, data["manifest_payload"])
        verified = verifier.verify(
            stored_record_bytes=model.canonical_json(rec),
            expected_kind="authority-discovery",
            expected_review_id="rev-1",
            expected_dispatch_id=None,
            expected_snapshot_epoch=snap["epoch"],
            expected_snapshot_fingerprint=snap["fingerprint"],
            expected_subject=model.canonical_json(subject),
            expected_tool_use_id=rec["tool_use_id"],
            expected_agent_id=None,
        )
        assert verified.kind == "authority-discovery"

    def test_acquire_refresh_epoch_plus_one_and_drift_reasons(self, tmp_path):
        # second enumeration on a different head + all feedback resolved
        git2 = FakeGit({"AGENTS.md": "# law"}, head="9" * 40, tree="8" * 40, diff="new-diff")
        gh2 = FakeGh(
            pr=_pr_meta(headRefOid="9" * 40),
            threads=[
                {
                    "id": "T1",
                    "isResolved": True,
                    "isOutdated": False,
                    "path": "a.py",
                    "line": 1,
                    "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
                }
            ],
        )
        gh2_a = FakeGh(
            threads=[
                {
                    "id": "T1",
                    "isResolved": False,
                    "isOutdated": False,
                    "path": "a.py",
                    "line": 1,
                    "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
                }
            ]
        )
        # first enumerate must include the unresolved thread so unresolved
        # digest differs after resolution; refresh re-uses the same scratch
        # root so the witness-policy digest stays constant
        scratch = _scratch(tmp_path)
        out1 = scratch / "acquire" / "e1"
        acq.enumerate_acquisition(
            run_git=FakeGit({"AGENTS.md": "# law"}),
            run_gh=gh2_a,
            repo_root=tmp_path,
            pr_number=7,
            out_dir=out1,
            scratch_dir=scratch,
        )
        d1 = json.loads((out1 / "data.json").read_text())
        old_snap = dict(d1["snapshot"])
        old_snap["fingerprint"] = model.snapshot_fingerprint(old_snap)
        out2 = scratch / "acquire" / "e2"
        summary2 = acq.enumerate_acquisition(
            run_git=git2,
            run_gh=gh2,
            repo_root=tmp_path,
            pr_number=7,
            out_dir=out2,
            scratch_dir=scratch,
            epoch=old_snap["epoch"] + 1,
        )
        _transcript_with_marker(scratch, summary2["enumeration_id"], out_dir=out2)
        src = _source(out2, scratch)
        payload = src.acquire(action="refresh-review-input", current_snapshot=old_snap)
        env = json.loads(payload.raw_data)
        snap = env["data"]["snapshot"]
        assert snap["epoch"] == old_snap["epoch"] + 1
        assert set(env["data"]["drift_reasons"]) == {
            "head_sha",
            "tree_sha",
            "diff_sha256",
            "authority_manifest",
            "feedback_history",
            "unresolved_feedback",
        }

    def test_refresh_requires_current_snapshot(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError):
            src.acquire(action="refresh-review-input", current_snapshot=None)

    def test_missing_marker_in_transcript_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, "0" * 64, out_dir=out_dir)
        src = _source(out_dir, scratch)
        with pytest.raises(Exception):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_acquired_dir_tamper_detected(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        ev = out_dir / "evidence"
        victim = next(ev.iterdir())
        if victim.name == "manifest.json":
            victim = next(p for p in ev.iterdir() if p.name != "manifest.json")
        raw = bytearray(victim.read_bytes())
        raw[0] ^= 0xFF
        victim.write_bytes(bytes(raw))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_resolve_between_enumerate_and_refresh(self, tmp_path):
        unresolved = [
            {
                "id": "T1",
                "isResolved": False,
                "isOutdated": False,
                "path": "a.py",
                "line": 1,
                "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
            }
        ]
        resolved = [dict(unresolved[0], isResolved=True)]
        _s1, out1, _ = _enumerate(tmp_path / "a", gh=FakeGh(threads=unresolved))
        _s2, out2, _ = _enumerate(tmp_path / "b", gh=FakeGh(threads=resolved))
        d2 = json.loads((out2 / "data.json").read_text())
        items = [a for a in d2["manifest_payload"]["authorities"] if a["kind"] == "review-feedback"]
        assert len(items) == 1
        # finding still ships open; lifecycle owns closure
        f = [x for x in d2["findings"] if x["source_kind"] == "feedback"]
        assert f and f[0]["disposition"] == "open"


class TestAcquireBindings:
    def test_find_segment_ignores_non_enumerate_exec(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        lines = [
            {
                "hook_event_name": "PostToolUse",
                "tool_name": "exec",
                "tool_input": {"command": f"cat {out_dir}/enumeration.json"},
                "tool_use_id": "exec_9",
                "session_id": "s1",
                "prompt_id": "p1",
                "tool_response": {
                    "success": True,
                    "output": f"enumeration-id: {summary['enumeration_id']}" + chr(10),
                    "error": None,
                },
            }
        ]
        t = Path(scratch) / "transcripts" / "s1.jsonl"
        t.write_text("".join(json.dumps(x) + chr(10) for x in lines), encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="missing-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_gh_edges_traversed_from_pr_body(self, tmp_path):
        body = "see <!-- authority:edge governs repo:extra.md --> for detail"
        gh = FakeGh(pr=_pr_meta(body=body))
        git = FakeGit({"AGENTS.md": "# law", "extra.md": "# extra"})
        summary, out_dir, _s = _enumerate(tmp_path, git=git, gh=gh)
        data = json.loads((out_dir / "data.json").read_text())
        locators = {e["locator"] for e in data["manifest_payload"]["authorities"]}
        assert "repo:extra.md" in locators
        assert summary["authority_count"] >= 3

    def test_enumerate_clears_stale_evidence(self, tmp_path):
        _s1, out_dir, scratch = _enumerate(tmp_path)
        stale = out_dir / "evidence" / "stale.bin"
        stale.write_bytes(b"leftover")
        (out_dir / "stale.txt").write_text("x")
        _s2, out_dir2, _ = _enumerate(tmp_path)
        assert out_dir2 == out_dir
        assert not stale.exists()
        assert not (out_dir / "stale.txt").exists()

    def test_acquire_rebinds_findings_from_evidence(self, tmp_path):
        threads = [
            {
                "id": "T1",
                "isResolved": False,
                "isOutdated": False,
                "path": "a.py",
                "line": 1,
                "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
            }
        ]
        summary, out_dir, scratch = _enumerate(tmp_path, gh=FakeGh(threads=threads))
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        data_path = out_dir / "data.json"
        data = json.loads(data_path.read_text())
        data["findings"] = [
            {
                "source_kind": "feedback",
                "source_id": "github:thread:fabricated",
                "disposition": "open",
            }
        ]
        data_path.write_text(json.dumps(data))
        env = _source(out_dir, scratch).acquire(action="freeze-review-input", current_snapshot=None)
        envelope = json.loads(env.raw_data)
        source_ids = {f["source_id"] for f in envelope["data"]["findings"]}
        assert "github:thread:fabricated" not in source_ids
        assert "github:thread:T1" in source_ids

    def test_item_from_raw_missing_id_fails_closed(self):
        raw = model.canonical_json({"kind": "thread", "node": {"no_id": True}})
        with pytest.raises(fbp.FeedbackPolicyError):
            fbp.item_from_raw(raw)
        raw = model.canonical_json({"kind": "mystery", "node": {"id": "X"}})
        with pytest.raises(fbp.FeedbackPolicyError):
            fbp.item_from_raw(raw)

    def test_thread_title_survives_null_and_blank_comments(self):
        # GitHub connections can carry null comment nodes (deleted or
        # permission-filtered comments) and a blank first body; the title
        # fallback must not crash finding materialization.
        pol = fbp.default_policy()
        for comments in ([None], [{"body": "   \n  "}], [], {"unexpected": 1}, "str"):
            node = {
                "id": "T-blank",
                "isResolved": False,
                "comments": {"pageInfo": {"hasNextPage": False}, "nodes": comments},
            }
            item = fbp.item_from_raw(model.canonical_json({"kind": "thread", "node": node}))
            finding = fbp.feedback_findings([item], policy=pol)[0]
            assert "review thread" in finding["title"]

    def test_evidence_diverging_from_bound_manifest_fails(self, tmp_path):
        # Tamper an evidence file AND update evidence/manifest.json so the
        # internal digest check passes - the record must still reconcile
        # against the witnessed manifest_payload entry.
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        ev_dir = out_dir / "evidence"
        ev_manifest = json.loads((ev_dir / "manifest.json").read_text())
        alias = next(a for a in ev_manifest if a.startswith("authority-"))
        victim = ev_dir / ev_manifest[alias]["file"]
        victim.write_bytes(b"tampered law")
        ev_manifest[alias]["sha256"] = model.sha256_hex(b"tampered law")
        (ev_dir / "manifest.json").write_text(json.dumps(ev_manifest))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_gh_list_response_fails_cleanly(self):
        def gh_list(_args):
            return 0, "[]", ""

        seed = SimpleNamespace(locator="gh:issue/12")
        with pytest.raises(acq.AcquisitionError):
            acq._load_seed_bytes(seed, run_git=None, run_gh=gh_list, base_sha=BASE, repo_id=REPO_ID, pr_meta=_pr_meta())
        seed = SimpleNamespace(locator="gh:doc/docs/x.md")
        with pytest.raises(acq.AcquisitionError):
            acq._load_seed_bytes(seed, run_git=None, run_gh=gh_list, base_sha=BASE, repo_id=REPO_ID, pr_meta=_pr_meta())

    def test_load_dir_reads_surrogate_locator(self, tmp_path):
        # data.json is written via canonical_json (surrogateescape), so a
        # non-UTF-8 file path leaves raw bytes in locator fields; the read
        # path must decode surrogateescape rather than crash.
        out = tmp_path / "acquire" / "latest"
        ev = out / "evidence"
        ev.mkdir(parents=True)
        loc = "repo:law\udcff.md"
        blob = b"authority bytes"
        sha = model.sha256_hex(blob)
        aid = "authority:" + model.sha256_json({"kind": "repo-law", "locator": loc})
        payload = {
            "authorities": [
                {
                    "authority_id": aid,
                    "kind": "repo-law",
                    "locator": loc,
                    "availability": "loaded",
                    "sha256": sha,
                    "failure_class": None,
                    "failure_sha256": None,
                }
            ]
        }
        payload_bytes = model.canonical_json(payload)
        (ev / "manifest-payload.bin").write_bytes(payload_bytes)
        (ev / "authority-0.bin").write_bytes(blob)
        (ev / "manifest.json").write_text(
            json.dumps(
                {
                    "manifest-payload": {
                        "file": "manifest-payload.bin",
                        "kind": "authority-manifest-payload",
                        "sha256": model.sha256_hex(payload_bytes),
                    },
                    "authority-0": {"file": "authority-0.bin", "kind": "authority", "sha256": sha},
                }
            )
        )
        data = {
            "snapshot": {"authority_manifest_sha256": "x"},
            "manifest_payload": payload,
            "authorities": [
                {
                    "authority_id": aid,
                    "kind": "repo-law",
                    "locator": loc,
                    "availability": "loaded",
                    "sha256": sha,
                    "evidence_id": "@authority-0",
                }
            ],
            "findings": [],
            "drift_reasons": None,
        }
        (out / "data.json").write_bytes(model.canonical_json(data))
        src = acq.LiveAuthorityDiscovery(
            acquisition_dir=out,
            witness_log_path=tmp_path / "w" / "log.jsonl",
            transcript_root=tmp_path / "t",
            review_id="r",
        )
        loaded, sources = src._load_dir()
        assert loaded["authorities"][0]["locator"] == loc
        assert any(s.alias == "authority-0" for s in sources)

    def test_failure_evidence_tamper_detected(self, tmp_path):
        # Default fixture leaves gh:issue/12 unavailable (FakeGh 404), so a
        # failure-* evidence file exists. Tamper it + the unbound sidecar +
        # the data record; the witnessed manifest entry still disagrees.
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        rec = next(r for r in data["authorities"] if r["availability"] == "unavailable")
        ev_manifest = json.loads((out_dir / "evidence" / "manifest.json").read_text())
        alias = rec["failure_evidence_id"][1:]
        (out_dir / "evidence" / ev_manifest[alias]["file"]).write_bytes(b"fabricated failure")
        ev_manifest[alias]["sha256"] = model.sha256_hex(b"fabricated failure")
        rec["failure_sha256"] = model.sha256_hex(b"fabricated failure")
        rec["failure_class"] = "tool-blocked"
        (out_dir / "evidence" / "manifest.json").write_text(json.dumps(ev_manifest))
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_dropped_authority_record_detected(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        data["authorities"] = data["authorities"][:-1]
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_authority_id_keyed_reconciliation(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        data["authorities"][0]["authority_id"] = "authority:" + "0" * 64
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_gh_doc_invalid_base64_is_authority_missing(self):
        def gh_bad_b64(args):
            return 0, json.dumps({"content": "abc"}), ""

        seed = SimpleNamespace(locator="gh:doc/docs/x.md")
        with pytest.raises(acq.AcquisitionError, match="authority-missing"):
            acq._load_seed_bytes(
                seed, run_git=None, run_gh=gh_bad_b64, base_sha=BASE, repo_id=REPO_ID, pr_meta=_pr_meta()
            )

    def test_gh_pr_view_list_response_is_tool_blocked(self, tmp_path):
        scratch = _scratch(tmp_path)
        gh = FakeGh(pr=[1, 2, 3])
        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=gh,
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=scratch / "acquire" / "latest",
                scratch_dir=scratch,
                epoch=1,
            )

    def test_graphql_list_response_is_policy_error(self):
        def gh_list(_args):
            return 0, "[1, 2, 3]", ""

        with pytest.raises(fbp.FeedbackPolicyError):
            fbp.enumerate_feedback(run_gh=gh_list, pr_url=PR_URL)

    def test_non_utf8_transcript_is_missing_source_not_traceback(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        bad = scratch / "transcripts" / "corrupt.jsonl"
        bad.write_bytes(bytes([0xFF, 0xFE]) + b" not utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises((acq.AcquisitionError, policy.WitnessVerificationError)):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_availability_flip_detected(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        rec = next(r for r in data["authorities"] if r["availability"] == "loaded")
        rec["availability"] = "unavailable"
        rec.pop("evidence_id", None)
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_non_string_evidence_id_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        data["authorities"][0]["evidence_id"] = 123
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_pr_meta_missing_field_is_tool_blocked(self, tmp_path):
        scratch = _scratch(tmp_path)
        gh = FakeGh(pr={"number": 7, "url": PR_URL})
        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=gh,
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=scratch / "acquire" / "latest",
                scratch_dir=scratch,
                epoch=1,
            )

    def test_surrogate_bytes_in_git_show_do_not_crash(self, tmp_path):
        git = FakeGit({"AGENTS.md": "# law caf\udcff"})
        summary, out_dir, _s = _enumerate(tmp_path, git=git)
        assert (out_dir / "data.json").exists()
        assert summary["enumeration_id"]

    def test_data_json_non_object_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "data.json").write_text("[]", encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_manifest_non_object_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "evidence" / "manifest.json").write_text("null", encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_entry_missing_kind_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        manifest_path = out_dir / "evidence" / "manifest.json"
        ev_manifest = json.loads(manifest_path.read_bytes())
        entry = next(iter(ev_manifest.values()))
        del entry["kind"]
        manifest_path.write_bytes(model.canonical_json(ev_manifest))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_data_json_missing_snapshot_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        del data["snapshot"]
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_data_json_missing_manifest_payload_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        del data["manifest_payload"]
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_authorities_non_list_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        data["authorities"] = {"a": 1}
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_manifest_duplicate_authority_id_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_bytes().decode("utf-8", "surrogateescape"))
        manifest_auths = data["manifest_payload"]["authorities"]
        assert len(manifest_auths) >= 2
        # A duplicated manifest entry with all unique records retained used to
        # slip past surjectivity: bound collapses the duplicate and len(seen)
        # still equals len(bound), so the duplicate's fields went unreconciled.
        manifest_auths.append(dict(manifest_auths[0]))
        (out_dir / "data.json").write_bytes(model.canonical_json(data))
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_gh_non_json_response_fails_cleanly(self):
        def gh_bad(_args):
            return 0, "<html>rate limited</html>", ""

        seed = SimpleNamespace(locator="gh:issue/12")
        with pytest.raises(acq.AcquisitionError, match="authority-missing"):
            acq._load_seed_bytes(seed, run_git=None, run_gh=gh_bad, base_sha=BASE, repo_id=REPO_ID, pr_meta=_pr_meta())
        seed = SimpleNamespace(locator="gh:doc/docs/x.md")
        with pytest.raises(acq.AcquisitionError, match="authority-missing"):
            acq._load_seed_bytes(seed, run_git=None, run_gh=gh_bad, base_sha=BASE, repo_id=REPO_ID, pr_meta=_pr_meta())

    def test_find_segment_record_missing_ids_fails_cleanly(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        lines = [
            {
                "hook_event_name": "PostToolUse",
                "tool_name": "exec",
                "tool_input": {"command": "py -3 reviewctl.py enumerate --state X/state.json --repo . --pr 7"},
                "prompt_id": "p1",
                "tool_response": {
                    "success": True,
                    "output": f"enumeration-id: {summary['enumeration_id']}" + chr(10),
                    "error": None,
                },
            }
        ]
        t = Path(scratch) / "transcripts" / "s1.jsonl"
        t.write_text("".join(json.dumps(x) + chr(10) for x in lines), encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="missing-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_data_json_non_json_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "data.json").write_text("not json {", encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_manifest_non_json_fails_closed(self, tmp_path):
        summary, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "evidence" / "manifest.json").write_text("<html></html>", encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_enumerate_missing_gh_binary_tool_blocked(self, tmp_path):
        scratch = _scratch(tmp_path)

        def no_gh(_args):
            raise FileNotFoundError("gh")

        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=no_gh,
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=scratch / "acquire" / "latest",
                scratch_dir=scratch,
                epoch=1,
            )

    def test_gh_text_tool_blocked_not_reclassified(self, tmp_path):
        # A tool outage fetching an edge-discovered gh locator must surface
        # tool-blocked, not degrade to authority-missing via _gh_text.
        scratch = _scratch(tmp_path)
        gh = FakeGh()

        def flaky(argv):
            if any("issues/34" in a for a in argv):
                raise PermissionError("binary gone")
            return gh(argv)

        git = FakeGit({"AGENTS.md": "<!-- authority:edge governs gh:issue/34 -->\n# law"})
        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=git,
                run_gh=flaky,
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=scratch / "acquire" / "latest",
                scratch_dir=scratch,
                epoch=1,
            )

    def test_enumerate_mid_run_gh_oserror_is_tool_blocked(self, tmp_path):
        # A runner that dies after the first call must still classify as
        # tool-blocked, not escape as io-error.
        scratch = _scratch(tmp_path)
        gh = FakeGh()
        calls = {"n": 0}

        def flaky(argv):
            calls["n"] += 1
            if calls["n"] > 1:
                raise PermissionError("binary gone")
            return gh(argv)

        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=flaky,
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=scratch / "acquire" / "latest",
                scratch_dir=scratch,
                epoch=1,
            )

    def test_enumerate_symlinked_out_dir_refused(self, tmp_path, monkeypatch):
        # A symlinked acquisition dir must be refused before rmtree can
        # follow it to a victim path.
        _s, out_dir, scratch = _enumerate(tmp_path)
        real_is_symlink = Path.is_symlink
        monkeypatch.setattr(
            Path,
            "is_symlink",
            lambda self: True if self == out_dir else real_is_symlink(self),
        )
        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=FakeGh(),
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=out_dir,
                scratch_dir=scratch,
                epoch=2,
            )
        assert (out_dir / "data.json").is_file()

    def test_enumerate_out_dir_outside_scratch_refused(self, tmp_path, monkeypatch):
        # A resolved path outside the scratch root (e.g. via a symlinked
        # ancestor) must be refused even when the name shape passes.
        _s, out_dir, scratch = _enumerate(tmp_path)
        victim = tmp_path / "victim" / "acquire" / "latest"
        victim.mkdir(parents=True)
        (victim / "keep.txt").write_text("x", encoding="utf-8")
        real_resolve = Path.resolve

        def bad_resolve(self, *a, **k):
            if self == out_dir:
                return victim
            return real_resolve(self, *a, **k)

        monkeypatch.setattr(Path, "resolve", bad_resolve)
        with pytest.raises(acq.AcquisitionError, match="tool-blocked"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=FakeGh(),
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=out_dir,
                scratch_dir=scratch,
                epoch=2,
            )
        assert (victim / "keep.txt").is_file()

    def test_enumerate_out_dir_replaced_by_file_is_tampered(self, tmp_path):
        _s, out_dir, scratch = _enumerate(tmp_path)
        shutil.rmtree(out_dir)
        out_dir.write_bytes(b"tampered")
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            acq.enumerate_acquisition(
                run_git=FakeGit({"AGENTS.md": "# law"}),
                run_gh=FakeGh(),
                repo_root=Path(tmp_path),
                pr_number=7,
                out_dir=out_dir,
                scratch_dir=scratch,
                epoch=2,
            )
        assert out_dir.is_file()

    def test_data_json_missing_fails_closed(self, tmp_path):
        _s, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "data.json").unlink()
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_manifest_missing_fails_closed(self, tmp_path):
        _s, out_dir, scratch = _enumerate(tmp_path)
        (out_dir / "evidence" / "manifest.json").unlink()
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_find_segment_skips_transcript_vanished_before_stat(self, tmp_path, monkeypatch):
        # A transcript deleted or locked between glob and stat must not
        # abort the scan: the surviving candidate still witnesses the run.
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        gone = Path(scratch) / "transcripts" / "gone.jsonl"
        gone.write_text("{}" + chr(10), encoding="utf-8")
        real_stat = Path.stat

        def flaky(self, *a, **k):
            if self.name == "gone.jsonl":
                raise FileNotFoundError("vanished between glob and stat")
            return real_stat(self, *a, **k)

        monkeypatch.setattr(Path, "stat", flaky)
        env = _source(out_dir, scratch).acquire(action="freeze-review-input", current_snapshot=None)
        assert env is not None

    def test_find_segment_all_transcripts_unstattable_is_tampered_source(self, tmp_path, monkeypatch):
        # stat failures on witnessed transcripts are tamper evidence, not
        # benign absence: an unreadable transcript cannot be audited.
        summary, out_dir, scratch = _enumerate(tmp_path)
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        real_stat = Path.stat

        def flaky(self, *a, **k):
            if self.suffix == ".jsonl" and self.parent.name == "transcripts":
                raise PermissionError("locked")
            return real_stat(self, *a, **k)

        monkeypatch.setattr(Path, "stat", flaky)
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_find_segment_unreadable_transcript_is_tampered_source(self, tmp_path):
        # A *.jsonl entry that stats fine but cannot be read (replaced by a
        # directory, or ACL'd between stat and read) is tamper evidence when
        # no surviving candidate witnesses the enumeration.
        _s, out_dir, scratch = _enumerate(tmp_path)
        bad = Path(scratch) / "transcripts" / "bad.jsonl"
        bad.mkdir(parents=True, exist_ok=True)
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)

    def test_evidence_manifest_traversal_file_field_is_tampered(self, tmp_path):
        # A manifest "file" pointing outside evidence/ must be refused before
        # any read - even when the recomputed digest would match.
        _s, out_dir, scratch = _enumerate(tmp_path)
        manifest_path = out_dir / "evidence" / "manifest.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        first_alias = next(iter(manifest))
        manifest[first_alias]["file"] = "../data.json"
        manifest[first_alias]["sha256"] = model.sha256_hex((out_dir / "data.json").read_bytes())
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_unavailable_record_null_failure_alias_is_tampered(self, tmp_path):
        # A record whose failure_evidence_id aliases nothing in the evidence
        # manifest must not pass on expected=None agreement.
        _s, out_dir, scratch = _enumerate(tmp_path)
        data = json.loads((out_dir / "data.json").read_text(encoding="utf-8"))
        rec = next(r for r in data["authorities"] if r["availability"] == "unavailable")
        witnessed = next(e for e in data["manifest_payload"]["authorities"] if e["authority_id"] == rec["authority_id"])
        rec["failure_evidence_id"] = "@ghost"
        rec["failure_sha256"] = witnessed["failure_sha256"] = None
        (out_dir / "data.json").write_text(json.dumps(data), encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_manifest_nul_file_field_is_tampered(self, tmp_path):
        # A tampered manifest "file" field carrying a NUL byte must hit
        # tampered-source: Path.is_file raises ValueError, not OSError.
        _s, out_dir, scratch = _enumerate(tmp_path)
        manifest_path = out_dir / "evidence" / "manifest.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        first_alias = next(iter(manifest))
        manifest[first_alias]["file"] = "evil\x00.bin"
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_evidence_file_unreadable_fails_closed(self, tmp_path, monkeypatch):
        _s, out_dir, scratch = _enumerate(tmp_path)
        ev_dir = out_dir / "evidence"
        victim = next(p for p in ev_dir.iterdir() if p.name != "manifest.json")
        real_read = Path.read_bytes

        def blocked(self, *a, **k):
            if self == victim:
                raise PermissionError("locked evidence")
            return real_read(self, *a, **k)

        monkeypatch.setattr(Path, "read_bytes", blocked)
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src._load_dir()

    def test_feedback_evidence_unreadable_after_digest_check_fails_closed(self, tmp_path, monkeypatch):
        # The manifest digest check reads the blob once; the feedback loop
        # re-reads it in acquire. Fail on the second read only so the
        # OSError path in the feedback loop is what fires.
        threads = [
            {
                "id": "T1",
                "isResolved": False,
                "isOutdated": False,
                "path": "a.py",
                "line": 1,
                "comments": {"pageInfo": {"hasNextPage": False}, "nodes": []},
            }
        ]
        summary, out_dir, scratch = _enumerate(tmp_path, gh=FakeGh(threads=threads))
        _transcript_with_marker(scratch, summary["enumeration_id"], out_dir=out_dir)
        ev_dir = out_dir / "evidence"
        manifest = json.loads((ev_dir / "manifest.json").read_text())
        fb_aliases = [a for a in manifest if a.startswith("feedback-")]
        assert fb_aliases
        victim = ev_dir / manifest[fb_aliases[0]]["file"]
        real_read = Path.read_bytes
        reads = {"n": 0}

        def flaky(self, *a, **k):
            if self == victim:
                reads["n"] += 1
                if reads["n"] > 1:
                    raise PermissionError("locked after digest check")
            return real_read(self, *a, **k)

        monkeypatch.setattr(Path, "read_bytes", flaky)
        src = _source(out_dir, scratch)
        with pytest.raises(acq.AcquisitionError, match="tampered-source"):
            src.acquire(action="freeze-review-input", current_snapshot=None)
