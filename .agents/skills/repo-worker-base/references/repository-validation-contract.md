# Repository Validation Contract

This is portable choreography. The consuming repository supplies its focused
validation map, tracked canonical commit gate, hosted workflow, and state
identifiers; this skill does not invent repository commands.

## Sequence

1. While editing, run the smallest focused slice that can falsify the current
   implementation claim.
2. Stage the intended final tree and make a normal commit. The tracked
   pre-commit hook is the hooked canonical gate and runs once over that staged
   state.
3. Reuse successful proof while the tested tree/head/staged state, relevant
   environment, command scope, and claim remain unchanged.
4. Keep the implementation PR Draft during local review and repair.
5. Promote to Ready only after current local canonical proof and local review
   are complete.
6. Let hosted CI provide remote confirmation. If it finds a failure the local
   gate should have caught, treat that as hook/hosted parity drift.

## state-bound evidence contract

Every validation record names the tested state, command and scope, relevant
environment, result, and the claim it supports. Repeat or broaden proof only
for a changed state, failure, unresolved concern, known nondeterminism,
environment drift, or a different proof claim. Conversation turn boundaries
do not determine evidence freshness.

## Test proportion

Preserve meaningful RED/GREEN behavior where it adds evidence. A helper with
no independent behavior may be covered transitively through its caller; do
not require a ceremonial direct test for every function or method.

## Publication and CI economics

Once a PR is authorized, Draft is the default creation state. Hosted CI must
skip Draft pull requests, and alternate automatic triggers must not bypass
that skip. A feature-branch push cannot silently start an equivalent paid
validation loop. Manual dispatch is a separate, explicitly classified path.
