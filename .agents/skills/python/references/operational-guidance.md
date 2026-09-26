# Python operational guidance

## When to apply

Use when the Python skill loaded and the question is deeper than a single sentence:

- choosing concurrency primitives,
- structuring pytest suites,
- applying type annotations,
- profiling and optimization.

## Asyncio and concurrency

- Use `async`/`await` for I/O-bound work (network, files, databases).
- Use `asyncio.gather` for independent coroutines; use `asyncio.TaskGroup` for structured cancellation when available.
- Run blocking CPU work in `loop.run_in_executor` or `ProcessPoolExecutor`.

## Testing with pytest

- Keep tests small and named after the behavior they assert.
- Use fixtures for setup/teardown; prefer `pytest.fixture(scope="function")` unless sharing expensive state.
- Parametrize data-driven cases.

## Type safety

- Annotate public functions; use `Optional`, `Union`, and generics where they remove ambiguity.
- Run `mypy --strict` in CI; suppress only with `# type: ignore[code]` and a comment.

## Profiling

- Profile before rewriting. Use `cProfile` for call counts, `line_profiler` for per-line cost.
- Optimize data structures and algorithms before micro-optimizations.

## Related references

- Python docs: https://docs.python.org/3/
- pytest docs: https://docs.pytest.org/en/stable/
- mypy docs: https://mypy.readthedocs.io/en/stable/
