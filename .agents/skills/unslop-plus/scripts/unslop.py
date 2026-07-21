#!/usr/bin/env python3
"""Generate a domain-specific anti-slop profile from local samples."""

from __future__ import annotations

import argparse
import asyncio
import hashlib
import html
import importlib.util
import json
import re
import shutil
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable


TOOL_VERSION = "0.1.0"
UPSTREAM = {
    "repo": "mshumer/unslop",
    "commit": "edcb62386d129c65e4395f0cfcc9168eb1ba2148",
    "license": "MIT",
}

TEXT_EXTENSIONS = {".txt", ".md", ".markdown"}
VISUAL_EXTENSIONS = {".html", ".htm", ".txt", ".md", ".markdown"}

COMMON_PATTERNS = [
    "in today's",
    "in an increasingly",
    "let's dive",
    "let's unpack",
    "at its core",
    "at the end of the day",
    "it is worth noting",
    "it's worth noting",
    "here's the thing",
    "here's why",
    "the reality is",
    "the bottom line",
    "in other words",
    "make no mistake",
    "this is where",
    "what does this mean",
    "game-changing",
    "transformative",
    "robust",
    "seamless",
    "unlock",
    "navigate",
    "landscape",
    "ecosystem",
    "delve",
    "crucial",
    "essential",
    "holistic",
]

VISUAL_PATTERNS = [
    "linear-gradient",
    "radial-gradient",
    "backdrop-filter",
    "blur(",
    "hero",
    "cta",
    "card",
    "pricing",
    "testimonial",
    "trusted by",
    "get started",
    "book a demo",
    "start free",
    "most popular",
    "navbar",
    "position: fixed",
]


@dataclass(frozen=True)
class Sample:
    name: str
    text: str
    source: str


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")
    return slug or "domain"


def normalize_space(value: str) -> str:
    return re.sub(r"\s+", " ", value).strip()


def words(value: str) -> list[str]:
    return re.findall(r"[a-z][a-z0-9'-]*", value.lower())


def sentences(value: str) -> list[str]:
    parts = re.split(r"(?<=[.!?])\s+", normalize_space(value))
    return [part.strip() for part in parts if len(part.strip()) >= 12]


def generate_prompts(domain: str, count: int, dtype: str) -> list[dict[str, object]]:
    text_tasks = [
        "Write a practical guide for a reader who already knows the basics.",
        "Draft an opinionated short essay with a clear point of view.",
        "Create an internal memo explaining a decision and its tradeoffs.",
        "Write a tutorial that avoids filler and gets to the actual steps.",
        "Create a launch announcement for a skeptical audience.",
        "Draft a troubleshooting note for a recurring operational problem.",
        "Write an expert critique of a common mistake in the field.",
        "Create a concise reference page with examples and caveats.",
    ]
    visual_tasks = [
        "Create a single-page HTML landing page.",
        "Create a pricing page with realistic product constraints.",
        "Create a dashboard-style HTML mockup.",
        "Create a product feature page for a specific buyer.",
        "Create a docs home page with navigation and examples.",
        "Create an onboarding screen with empty, loading, and error states.",
        "Create a comparison page against an incumbent product.",
        "Create a workflow tool interface for repeated daily use.",
    ]
    tasks = visual_tasks if dtype == "visual" else text_tasks
    prompts: list[dict[str, object]] = []
    for index in range(count):
        task = tasks[index % len(tasks)]
        prompts.append(
            {
                "id": f"prompt_{index:04d}",
                "domain": domain,
                "type": dtype,
                "prompt": f"{task} Domain: {domain}. Make it specific enough to reveal default patterns.",
            }
        )
    return prompts


def fixture_samples(domain: str, dtype: str) -> list[Sample]:
    if dtype == "visual":
        body = [
            """<html><body><nav class="navbar">Logo <button>Get Started</button></nav><section class="hero"><h1>Unlock seamless growth</h1><p>In today's fast-moving landscape, teams need robust workflows.</p><div class="card">Trusted by 2,400+ teams</div></section><section class="pricing"><div class="card">Most Popular</div></section></body></html>""",
            """<html><body><nav class="navbar">Brand <button>Book a Demo</button></nav><section class="hero"><h1>Transform your workflow</h1><p>At its core, this platform helps teams navigate complexity.</p><div class="card">Trusted by leading teams</div></section><section class="testimonial">Loved by operators</section></body></html>""",
            """<html><body><header class="navbar">Product <button>Start Free</button></header><main class="hero"><h1>Seamless operations for modern teams</h1><p>The bottom line: robust tooling unlocks productivity.</p><div class="pricing card">Most Popular plan</div></main></body></html>""",
        ]
    else:
        body = [
            f"In today's {domain} landscape, teams need robust processes. Let's dive into why this matters. The bottom line is that seamless workflows unlock better outcomes.",
            f"At its core, {domain} is about helping teams navigate complexity. Here's the thing: robust systems are essential. Let's unpack the practical steps.",
            f"In an increasingly complex {domain} world, it is worth noting that teams need seamless collaboration. The reality is that the right ecosystem can unlock progress.",
            f"The bottom line for {domain}: avoid vague landscapes and focus on concrete work. But here's why this matters: robust planning changes outcomes.",
        ]
    return [Sample(name=f"fixture_{index:04d}", text=text, source="fixture") for index, text in enumerate(body)]


def load_samples(samples_dir: Path | None, inline_samples: list[str], dtype: str, use_fixtures: bool, domain: str) -> list[Sample]:
    loaded: list[Sample] = []
    if samples_dir:
        allowed = VISUAL_EXTENSIONS if dtype == "visual" else TEXT_EXTENSIONS
        for path in sorted(samples_dir.iterdir()):
            if not path.is_file() or path.suffix.lower() not in allowed:
                continue
            loaded.append(Sample(name=path.stem, text=path.read_text(encoding="utf-8"), source=str(path)))
    for index, sample in enumerate(inline_samples):
        loaded.append(Sample(name=f"inline_{index:04d}", text=sample, source="inline"))
    if use_fixtures:
        loaded.extend(fixture_samples(domain, dtype))
    return loaded


def prepare_output(out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    existing_children = list(out.iterdir())
    for child in existing_children:
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()
    for child in ("samples", "before-after"):
        (out / child).mkdir(parents=True, exist_ok=True)


def ensure_safe_output_dir(out: Path) -> None:
    resolved_out = out.resolve()
    cwd = Path.cwd().resolve()
    if resolved_out == cwd or cwd.is_relative_to(resolved_out):
        raise ValueError(
            f"Refusing to clean output directory {out!s}: choose a dedicated generated directory outside the current working tree."
        )


def cleanup_output_dir(out: Path, *, force_cleanup: bool) -> None:
    out.mkdir(parents=True, exist_ok=True)
    if any(out.iterdir()) and not force_cleanup:
        raise ValueError(f"Refusing to clean non-empty output directory {out!s} without --force-cleanup.")
    for child in list(out.iterdir()):
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()


def write_json(path: Path, data: object) -> None:
    path.write_text(json.dumps(data, indent=2, sort_keys=False) + "\n", encoding="utf-8")


def write_samples(out: Path, samples: list[Sample], dtype: str) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    ext = ".html" if dtype == "visual" else ".txt"
    for index, sample in enumerate(samples):
        filename = f"sample_{index:04d}{ext}"
        path = out / "samples" / filename
        path.write_text(sample.text, encoding="utf-8")
        records.append(
            {
                "id": f"sample_{index:04d}",
                "path": f"samples/{filename}",
                "source": sample.source,
                "bytes": len(sample.text.encode("utf-8")),
                "sha256": hashlib.sha256(sample.text.encode("utf-8")).hexdigest(),
            }
        )
    return records


def ngram_counts(sample_words: list[list[str]], n: int) -> Counter[str]:
    counts: Counter[str] = Counter()
    stop = {"the", "and", "for", "that", "with", "this", "from", "your", "you", "are", "can", "into"}
    for tokens in sample_words:
        for index in range(0, max(0, len(tokens) - n + 1)):
            gram_tokens = tokens[index : index + n]
            if all(token in stop for token in gram_tokens):
                continue
            counts[" ".join(gram_tokens)] += 1
    return counts


def phrase_counts(samples: list[Sample], phrases: Iterable[str]) -> Counter[str]:
    counts: Counter[str] = Counter()
    for sample in samples:
        lowered = sample.text.lower()
        for phrase in phrases:
            found = lowered.count(phrase)
            if found:
                counts[phrase] += found
    return counts


def sentence_openers(samples: list[Sample]) -> Counter[str]:
    counts: Counter[str] = Counter()
    for sample in samples:
        for sentence in sentences(sample.text):
            opener = " ".join(words(sentence)[:4])
            if opener:
                counts[opener] += 1
    return counts


def markdown_headings(samples: list[Sample]) -> Counter[str]:
    counts: Counter[str] = Counter()
    for sample in samples:
        for line in sample.text.splitlines():
            stripped = line.strip()
            if stripped.startswith("#"):
                counts[normalize_space(stripped.lstrip("#").strip()).lower()] += 1
    return counts


def html_structure_counts(samples: list[Sample]) -> Counter[str]:
    counts: Counter[str] = Counter()
    for sample in samples:
        lowered = sample.text.lower()
        for tag in ("nav", "header", "section", "main", "footer", "button", "article"):
            counts[f"<{tag}> tags"] += len(re.findall(rf"<{tag}\b", lowered))
        for css_class in ("hero", "card", "pricing", "testimonial", "cta", "navbar"):
            counts[f"`{css_class}` class/name"] += lowered.count(css_class)
    return Counter({key: value for key, value in counts.items() if value})


def analyze(samples: list[Sample], domain: str, dtype: str) -> dict[str, object]:
    sample_words = [words(sample.text) for sample in samples]
    repeated_ngrams: Counter[str] = Counter()
    for n in (2, 3, 4):
        repeated_ngrams.update({key: value for key, value in ngram_counts(sample_words, n).items() if value >= 2})

    pattern_source = COMMON_PATTERNS + (VISUAL_PATTERNS if dtype == "visual" else [])
    repeated_phrases = phrase_counts(samples, pattern_source)
    openers = Counter({key: value for key, value in sentence_openers(samples).items() if value >= 2})
    headings = Counter({key: value for key, value in markdown_headings(samples).items() if value >= 2})
    html_counts = html_structure_counts(samples) if dtype == "visual" else Counter()

    sample_hits: defaultdict[str, set[str]] = defaultdict(set)
    for sample in samples:
        lowered = sample.text.lower()
        for phrase in repeated_phrases:
            if phrase in lowered:
                sample_hits[phrase].add(sample.name)

    return {
        "domain": domain,
        "type": dtype,
        "sample_count": len(samples),
        "word_count": sum(len(tokens) for tokens in sample_words),
        "repeated_phrases": repeated_phrases.most_common(20),
        "phrase_sample_counts": {phrase: len(names) for phrase, names in sample_hits.items()},
        "repeated_ngrams": repeated_ngrams.most_common(20),
        "sentence_openers": openers.most_common(12),
        "markdown_headings": headings.most_common(12),
        "html_structure": html_counts.most_common(20),
    }


def bullet_lines(items: list[tuple[str, int]], prefix: str, sample_counts: dict[str, int] | None = None) -> list[str]:
    lines: list[str] = []
    for value, count in items:
        sample_text = ""
        if sample_counts and value in sample_counts:
            sample_text = f" across {sample_counts[value]} sample(s)"
        lines.append(f"- {prefix} `{value}` appeared {count} time(s){sample_text}.")
    return lines


def write_analysis(out: Path, result: dict[str, object]) -> None:
    domain = str(result["domain"])
    dtype = str(result["type"])
    phrase_counts_by_sample = result.get("phrase_sample_counts", {})
    if not isinstance(phrase_counts_by_sample, dict):
        phrase_counts_by_sample = {}

    lines = [
        f"# Unslop analysis for {domain}",
        "",
        f"- Domain: {domain}",
        f"- Run type: {dtype}",
        f"- Samples analyzed: {result['sample_count']}",
        f"- Total words/tokens counted: {result['word_count']}",
        "",
        "## Counted repeated phrases",
    ]
    repeated_phrases = list(result["repeated_phrases"])  # type: ignore[index]
    if repeated_phrases:
        lines.extend(bullet_lines(repeated_phrases, "Avoid default phrase", phrase_counts_by_sample))
    else:
        lines.append("- No stock phrase crossed the counted threshold; inspect repeated n-grams and structure instead.")

    lines.extend(["", "## Repeated wording clusters"])
    repeated_ngrams = list(result["repeated_ngrams"])  # type: ignore[index]
    if repeated_ngrams:
        lines.extend(bullet_lines(repeated_ngrams[:12], "Repeated wording cluster", None))
    else:
        lines.append("- No repeated wording clusters crossed the threshold.")

    lines.extend(["", "## Repeated openings and structure"])
    sentence_items = list(result["sentence_openers"])  # type: ignore[index]
    heading_items = list(result["markdown_headings"])  # type: ignore[index]
    html_items = list(result["html_structure"])  # type: ignore[index]
    if sentence_items:
        lines.extend(bullet_lines(sentence_items, "Repeated sentence opener", None))
    if heading_items:
        lines.extend(bullet_lines(heading_items, "Repeated heading", None))
    if html_items:
        lines.extend(bullet_lines(html_items, "Repeated visual/code structure", None))
    if not sentence_items and not heading_items and not html_items:
        lines.append("- No repeated opener, heading, or visual structure crossed the threshold.")

    lines.extend(
        [
            "",
            "## Domain-specific interpretation",
            f"The `{domain}` samples show defaults that should be removed before this profile is reused. Prefer constraints that make future outputs more concrete for `{domain}` instead of replacing these defaults with another generic tone.",
            "",
            "## Validation notes",
            "- This analysis is generated from local samples only.",
            "- Treat the generated skill as a draft until reviewed against the sample set.",
        ]
    )
    (out / "analysis.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def make_avoid_bullets(result: dict[str, object]) -> list[str]:
    bullets: list[str] = []
    for phrase, _count in list(result["repeated_phrases"])[:10]:  # type: ignore[index]
        bullets.append(f"- Do not use `{phrase}` or close variants as a default move.")
    for phrase, _count in list(result["repeated_ngrams"])[:8]:  # type: ignore[index]
        if len(phrase.split()) >= 3:
            bullets.append(f"- Do not repeat the wording cluster `{phrase}` across outputs.")
    for opener, _count in list(result["sentence_openers"])[:5]:  # type: ignore[index]
        bullets.append(f"- Do not keep opening sentences with `{opener}`.")
    for item, _count in list(result["html_structure"])[:8]:  # type: ignore[index]
        bullets.append(f"- Do not rely on the repeated visual/code structure {item} unless the prompt specifically requires it.")
    if len(bullets) < 8:
        domain = str(result["domain"])
        bullets.extend(
            [
                f"- Do not start `{domain}` work with broad scene-setting before the specific subject.",
                f"- Do not pad `{domain}` outputs with generic credibility, urgency, or transformation claims.",
                f"- Do not use vague adjectives when a concrete `{domain}` constraint, example, or tradeoff would fit.",
                "- Do not add a closing summary that restates the obvious in grander language.",
                "- Do not swap these defaults for a single new house style; choose based on the actual task.",
            ]
        )
    return bullets[:18]


def write_skill(out: Path, result: dict[str, object]) -> None:
    domain = str(result["domain"])
    bullets = make_avoid_bullets(result)
    lines = [
        f"# Unslop profile for {domain}",
        "",
        "Generated from local samples. Review before installing as durable instructions.",
        "",
        "## Avoid these observed defaults",
        *bullets,
        "",
        "## Use instead",
        f"- Start from the specific `{domain}` task, audience, constraints, and evidence.",
        "- Vary structure only when the task benefits from it, not as decoration.",
        "- Replace broad claims with concrete examples, caveats, numbers, or artifacts from the prompt.",
        "- Keep the profile negative-first: remove repeated defaults before adding new style preferences.",
        "",
        "## Review rule",
        "If a future output starts to match several avoided patterns, stop and rewrite from the domain facts rather than polishing the generic draft.",
    ]
    (out / "skill.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def visual_smoke(dtype: str) -> dict[str, object]:
    if dtype != "visual":
        return {"requested": False, "status": "not_requested", "reason": ""}
    if importlib.util.find_spec("playwright") is None:
        return {"requested": True, "status": "skipped", "reason": "Playwright Python package missing", "browser_candidates": []}
    browser_candidates = [
        candidate
        for candidate in (
            shutil.which("chromium"),
            shutil.which("chromium-browser"),
            shutil.which("google-chrome"),
            shutil.which("google-chrome-stable"),
        )
        if candidate
    ]
    return {"requested": True, "status": "available", "reason": "", "browser_candidates": browser_candidates}


async def render_visual_samples(out: Path, smoke: dict[str, object]) -> dict[str, object]:
    if smoke.get("status") != "available":
        return {"status": "skipped", "reason": smoke.get("reason", "")}
    try:
        from playwright.async_api import async_playwright
    except Exception as exc:  # pragma: no cover - depends on optional package
        return {"status": "skipped", "reason": f"Playwright import failed: {exc}"}

    browser_candidates = list(smoke.get("browser_candidates") or [])
    screenshots = out / "before-after"
    rendered = 0
    browser = None
    try:
        async with async_playwright() as playwright:  # pragma: no cover - optional dependency
            launch_errors: list[str] = []
            launch_attempts = [{"headless": True}]
            launch_attempts.extend({"headless": True, "executable_path": candidate} for candidate in browser_candidates)
            for launch_kwargs in launch_attempts:
                try:
                    browser = await playwright.chromium.launch(**launch_kwargs)
                    break
                except Exception as exc:
                    launch_errors.append(str(exc))
            if browser is None:
                return {"status": "failed", "reason": f"Visual browser launch failed: {'; '.join(launch_errors)}"}
            for sample in sorted((out / "samples").glob("*.html")):
                page = await browser.new_page(viewport={"width": 1280, "height": 900})
                try:
                    await page.goto(sample.resolve().as_uri())
                    await page.screenshot(path=str(screenshots / f"{sample.stem}.png"), full_page=True)
                finally:
                    await page.close()
                rendered += 1
    except Exception as exc:  # pragma: no cover - optional dependency
        return {"status": "failed", "reason": f"Visual render failed: {exc}"}
    finally:
        if browser is not None:
            await browser.close()
    return {"status": "ran", "screenshots": rendered, "path": "before-after/"}


def validate_result(out: Path, domain: str, dtype: str, sample_count: int, visual_status: dict[str, object]) -> tuple[bool, list[str]]:
    issues: list[str] = []
    analysis = (out / "analysis.md").read_text(encoding="utf-8") if (out / "analysis.md").exists() else ""
    skill = (out / "skill.md").read_text(encoding="utf-8") if (out / "skill.md").exists() else ""
    if sample_count < 2:
        issues.append("At least two samples are required for a useful repeated-pattern analysis.")
    if domain.lower() not in analysis.lower():
        issues.append("analysis.md does not mention the requested domain.")
    if len(re.findall(r"\b\d+\b", analysis)) < 3:
        issues.append("analysis.md is not counted enough; expected multiple numeric counts.")
    avoid_count = len(re.findall(r"\b(do not|avoid|never)\b", skill.lower()))
    if avoid_count < 6:
        issues.append("skill.md is too weak; expected at least six avoid/do-not instructions.")
    if "Generated from local samples" not in skill:
        issues.append("skill.md does not mark itself as a reviewed draft from local samples.")
    if dtype == "visual":
        visual_state = visual_status.get("status")
        if visual_state == "failed":
            issues.append(f"visual mode render failed: {visual_status.get('reason', 'unknown reason')}")
        elif visual_state not in {"ran", "skipped"}:
            issues.append("visual mode did not record ran, skipped, or failed visual evidence.")
        if visual_state == "ran":
            screenshot_count = visual_status.get("screenshots")
            if not isinstance(screenshot_count, int) or screenshot_count < 1:
                issues.append("visual mode reported ran but did not record any screenshots.")
            screenshot_files = list((out / "before-after").glob("*.png"))
            if isinstance(screenshot_count, int) and len(screenshot_files) != screenshot_count:
                issues.append("visual screenshot count does not match before-after files.")
    return (not issues, issues)


def write_validation(out: Path, passed: bool, issues: list[str], visual_status: dict[str, object]) -> None:
    lines = ["# Unslop validation", "", f"- Status: {'passed' if passed else 'failed'}"]
    if issues:
        lines.extend(["", "## Issues"])
        lines.extend(f"- {issue}" for issue in issues)
    lines.extend(
        [
            "",
            "## Visual evidence",
            f"- Status: {visual_status.get('status', 'not_requested')}",
        ]
    )
    reason = visual_status.get("reason")
    if reason:
        lines.append(f"- Reason: {reason}")
    if visual_status.get("screenshots") is not None:
        lines.append(f"- Screenshots: {visual_status['screenshots']}")
    (out / "validation.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_before_after_note(out: Path, prompts: list[dict[str, object]], skill_path: Path) -> None:
    comparison = out / "before-after"
    comparison.mkdir(exist_ok=True)
    prompt = prompts[len(prompts) // 2]["prompt"] if prompts else "Use the generated profile on a representative task."
    (comparison / "test-prompt.txt").write_text(str(prompt) + "\n", encoding="utf-8")
    (comparison / "README.md").write_text(
        "\n".join(
            [
                "# Before/after review",
                "",
                "This package does not call a model provider directly. To compare before and after output, run the test prompt once without the generated profile and once with the profile prepended.",
                "",
                f"- Test prompt: `test-prompt.txt`",
                f"- Draft profile: `{skill_path.name}`",
            ]
        )
        + "\n",
        encoding="utf-8",
    )


def build_manifest(
    args: argparse.Namespace,
    prompts: list[dict[str, object]],
    sample_records: list[dict[str, object]],
    visual_smoke_result: dict[str, object],
    visual_evidence: dict[str, object],
    validation_passed: bool,
    validation_issues: list[str],
) -> dict[str, object]:
    provider_mode = "fixture" if args.fixture_samples else "sample-folder"
    if args.sample:
        provider_mode = "inline-samples" if not args.samples_dir else "sample-folder+inline"
    if args.prompts_only:
        provider_mode = "prompt-generation-only"
        return {
            "tool": "asset-marketplace-unslop",
            "tool_version": TOOL_VERSION,
            "created_at": utc_now(),
            "domain": args.domain,
            "type": args.type,
            "output_contract": "unslop-prompts-only/v1",
            "upstream_provenance": UPSTREAM,
            "parameters": {
                "count": args.count,
                "output": str(args.output),
                "skip_comparison": args.skip_comparison,
            },
            "provider_orchestration_mode": provider_mode,
            "prompts": {"path": "prompts.json", "count": len(prompts)},
            "samples": {"path": "samples/", "count": len(sample_records), "items": sample_records},
            "visual_smoke": visual_smoke_result,
            "visual_evidence": visual_evidence,
            "validation": {"status": "not_applicable", "passed": None, "issues": [], "path": None},
            "outputs": {
                "prompts": "prompts.json",
            },
        }
    return {
        "tool": "asset-marketplace-unslop",
        "tool_version": TOOL_VERSION,
        "created_at": utc_now(),
        "domain": args.domain,
        "type": args.type,
        "output_contract": "unslop-output/v1",
        "upstream_provenance": UPSTREAM,
        "parameters": {
            "count": args.count,
            "output": str(args.output),
            "skip_comparison": args.skip_comparison,
        },
        "provider_orchestration_mode": provider_mode,
        "prompts": {"path": "prompts.json", "count": len(prompts)},
        "samples": {"path": "samples/", "count": len(sample_records), "items": sample_records},
        "visual_smoke": visual_smoke_result,
        "visual_evidence": visual_evidence,
        "validation": {
            "status": "passed" if validation_passed else "failed",
            "passed": validation_passed,
            "issues": validation_issues,
            "path": "validation.md",
        },
        "outputs": {
            "analysis": "analysis.md",
            "skill": "skill.md",
            "before_after": "before-after/",
        },
    }


def run(args: argparse.Namespace) -> int:
    out = args.output
    try:
        ensure_safe_output_dir(out)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    prompts = generate_prompts(args.domain, args.count, args.type)

    if args.prompts_only:
        try:
            cleanup_output_dir(out, force_cleanup=args.force_cleanup)
        except ValueError as exc:
            print(str(exc), file=sys.stderr)
            return 2
        write_json(out / "prompts.json", prompts)
        visual = visual_smoke(args.type)
        manifest = build_manifest(args, prompts, [], visual, {"status": "not_requested"}, False, [])
        write_json(out / "manifest.json", manifest)
        print(f"Wrote prompts to {out / 'prompts.json'}")
        return 0

    samples = load_samples(args.samples_dir, args.sample, args.type, args.fixture_samples, args.domain)
    if not samples:
        print("No samples found. Use --samples-dir, --sample, --fixture-samples, or --prompts-only.", file=sys.stderr)
        return 2

    try:
        cleanup_output_dir(out, force_cleanup=args.force_cleanup)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    prepare_output(out)
    write_json(out / "prompts.json", prompts)
    sample_records = write_samples(out, samples, args.type)
    result = analyze(samples, args.domain, args.type)
    write_analysis(out, result)
    write_skill(out, result)

    smoke = visual_smoke(args.type)
    visual_status = asyncio.run(render_visual_samples(out, smoke)) if args.type == "visual" else {"status": "not_requested"}
    passed, issues = validate_result(out, args.domain, args.type, len(samples), visual_status)
    write_validation(out, passed, issues, visual_status)
    if not args.skip_comparison:
        write_before_after_note(out, prompts, out / "skill.md")
    manifest = build_manifest(args, prompts, sample_records, smoke, visual_status, passed, issues)
    write_json(out / "manifest.json", manifest)

    print(f"Wrote analysis to {out / 'analysis.md'}")
    print(f"Wrote draft skill to {out / 'skill.md'}")
    print(f"Wrote manifest to {out / 'manifest.json'}")
    print(f"Validation: {'passed' if passed else 'failed'}")
    if issues:
        for issue in issues:
            print(f"- {issue}", file=sys.stderr)
    return 0 if passed else 1


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--domain", required=True, help="Domain to analyze.")
    parser.add_argument("--type", choices=("text", "visual"), default="text", help="Run type.")
    parser.add_argument("--count", type=int, default=12, help="Number of prompts to write.")
    parser.add_argument("--samples-dir", type=Path, help="Directory containing local sample files.")
    parser.add_argument("--sample", action="append", default=[], help="Inline sample text. May be repeated.")
    parser.add_argument("--fixture-samples", action="store_true", help="Use bundled smoke-test samples.")
    parser.add_argument("--prompts-only", action="store_true", help="Only write prompts.json and a manifest.")
    parser.add_argument("--output", type=Path, default=Path("unslop-output"), help="Output directory.")
    parser.add_argument("--skip-comparison", action="store_true", help="Do not write before-after review notes.")
    parser.add_argument("--force-cleanup", action="store_true", help="Allow cleanup of a non-empty output directory.")
    args = parser.parse_args(argv)
    if args.count < 1:
        parser.error("--count must be at least 1")
    if args.samples_dir and not args.samples_dir.exists():
        parser.error(f"--samples-dir does not exist: {args.samples_dir}")
    return args


def main(argv: list[str] | None = None) -> int:
    return run(parse_args(argv or sys.argv[1:]))


if __name__ == "__main__":
    raise SystemExit(main())
