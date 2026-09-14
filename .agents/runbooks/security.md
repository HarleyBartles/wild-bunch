# Security runbook

## When

A Wild Bunch change affects tool side effects, permissions, secrets, hidden
truth, or a sensitive mutation boundary.

## Required skills

- `/connector-safety` for connector and tool mutations.
- `/risk-gates` when scope, authority, source truth, or safety needs a gate.

## Composition

Use `/risk-gates` before a sensitive action, then `/connector-safety` for the
approved discover-read-write-verify sequence.

## Doctrine and contracts

[Architecture guardrails](../doctrine/architecture-guardrails.md) and
[gameplay invariants](../doctrine/gameplay-invariants.md) protect backend and
hidden-truth boundaries.

## Local commands and paths

Use the relevant focused tests and [testing](testing.md); never place secrets in
repo files or command output.

## Evidence contract

The return identifies the authority used, exact mutation, readback, and any
unresolved exposure or permission boundary.

## Prohibited combinations

- Do not bundle sensitive mutations with unrelated writes.
- Do not expose hidden game truth through player-facing APIs.
