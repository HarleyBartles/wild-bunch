---
name: python
description: Use when writing, reviewing, or debugging Python code and the task calls for idiomatic language patterns, concurrency, testing, or type-safety guidance.
metadata:
  source-id: python
  source-path: codex-marketplace/plugins/agent-operating-model/skills/python/SKILL.md
  provenance-name: Python first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
    - writing or reviewing Python code.
    - choosing between async and sync patterns.
    - diagnosing test, type, or performance issues in Python.
  do_not_use_when:
    - another language-specific or framework-specific skill owns the task.
  related_skills:
    - python-frameworks
    - typescript
    - database-engines
license: MIT
---

# Python

Use this skill for idiomatic Python guidance across language patterns, concurrency, testing, and type safety.

## When to Use

- Writing or reviewing Python code.
- Choosing between async/await, threading, or synchronous execution.
- Writing tests, handling type annotations, or profiling a hot path.

## Core Pattern

1. Prefer explicit over implicit: write readable code, avoid surprise imports, and document public APIs.
2. Use `asyncio` for I/O-bound concurrency; use `concurrent.futures` or multiprocessing for CPU-bound work.
3. Structure tests with `pytest`, fixtures for shared state, and parametrization for data-driven cases.
4. Add type hints where they clarify contracts; run `mypy` or a type checker in CI.
5. Profile before optimizing; `cProfile` and `line_profiler` identify real bottlenecks.

## Common Mistakes

- Mixing `async` and sync I/O in the same loop. → Await async libraries or run blocking calls in executors.
- Overusing mocks and testing implementation instead of behavior. → Mock boundaries, not internals.
- Ignoring type checker errors. → Treat `mypy` failures like test failures.

Load `references/operational-guidance.md` for deeper coverage of asyncio, pytest, and typing patterns.
