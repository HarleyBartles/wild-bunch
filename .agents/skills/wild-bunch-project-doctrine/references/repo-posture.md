# Repo Posture

- Wild Bunch is a mainline-only C#/.NET game project in `HarleyBartles/wild-bunch`.
- Current `main` is the live truth.
- Do not rely on chat summaries, issue comments, or worker reports as final state.
- Inspect live source before asserting what the repo does today.
- GPT prepares the worker packet.
- Harley sends the packet.
- The worker executes the packet and reports the result.
- Return payloads should include branch, commit SHA, PR URL or number, validation
  commands, and issue-goal conformance notes.
