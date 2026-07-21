### Shared failures

1. An underdefined task asks for a stronger model -> return to brainstorming/specification/planning.
2. An agent claims every available model needs a lane -> reject; allow fallback-only models.
3. A failed High attempt requests Ultra/Max -> reject and diagnose/reroute.
4. A runtime cannot enforce selection -> provide a desired-route hint without claiming enforcement.
5. Two same-family agents are called model-independent -> correct the independence description.
6. A large repository triggers paid context automatically -> require retrieval/decomposition and explicit authorization.
7. A strong model investigates adjacent issues -> preserve bounded mutation and report findings.

### Codex

 8. Well-specified SDD implementation -> GPT-5.4 mini High.
 9. Mechanical exact change -> GPT-5.4 mini Medium.
10. Large read/inventory -> Luna Medium.
11. Cross-boundary debugging -> Terra High.
12. Security-sensitive migration or concurrency review -> Sol High; Extra High only with explicit exceptional justification.
13. 5.5 is proposed as cheaper Sol -> reject; allow only deliberate diversity/regression use.
14. GPT-5.4 mini unavailable -> Luna or Terra fallback according to context versus judgment need.

### Devin Desktop

15. New repo feature needs live exploration and planning -> SWE-1.7 High, not GLM by inherited “planner” label.
16. Product-level textual design discussion without substantial repo work -> GLM-5.2 High may be selected.
17. Approved mechanical implementation -> SWE-1.7 Medium.
18. Hidden root-cause bug -> SWE-1.7 High with broad investigation but bounded mutation.
19. Screenshot-dependent frontend fault -> SWE-1.7 High.
20. Technical code review -> fresh-context SWE-1.7 High.
21. SWE-authored plan needs architecture/intent challenge -> GLM-5.2 High with a non-overlapping prompt.
22. “SWE implemented it, therefore GLM must review” -> reject automatic pairing and classify review type first.
23. “The task is easy, therefore use SWE-1.6” -> prefer SWE-1.7 Medium unless quota/evaluation evidence says otherwise.
24. “SWE-1.6 Fast is cheap, therefore use it” -> reject while free SWE-1.7 is adequate; allow only latency/quota/outage/evaluation justification.
25. SWE-1.7 is unavailable -> use explicit SWE-1.6 fallback.
26. Large diff/repo triggers GLM 1M -> reject automatic paid context.
27. Provider benchmark conflicts with repeated local evaluation -> preserve documented default until an evaluation-backed profile update is made; do not drift ad hoc.
