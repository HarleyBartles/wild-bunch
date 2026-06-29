# Legacy Plan B

Read only when the Linear-backed worker route is unavailable, explicitly rejected by Harley, not connected for the project, or blocked by the golden gate while a non-Linear worker handoff is still required.

Plan B is compact guidance, not a parallel governance stack.

## Use Plan B for

- Linear outage or connector failure;
- current cloud-agent route unavailable for the repo;
- repo/project not connected to the current cloud-agent route;
- external worker route that cannot read Linear;
- Harley explicitly asks for legacy/non-Linear handoff.

## Plan B shape

Create the smallest durable handoff that names:

- target repo/surface;
- goal;
- scope and guardrails;
- validation expectations;
- return evidence;
- publication expectation.

Do not resurrect large YAML packet doctrine unless the target worker requires that exact format.

## Return to Plan A

As soon as the Linear-backed worker route is available again, move active durable state back into Linear and resume the normal state machine.
