# Operating Contract

This is the shared, model-agnostic contract for composed workflow skills. It applies to Luna, Terra, Sol, Astra, and other supported runtimes.

## Authority order

1. Explicit human instruction.
2. Repository canon and policy for the surface being changed.
3. The owning skill's applicability and safety contract.
4. Caller or runbook routing advice.
5. Generic defaults.

The owning skill defines applicability and entry conditions. A caller may request a capability, but cannot bypass, weaken, or strengthen the owner's gate. Portable skills do not outrank repository canon.

## Semantic authority

Evidence can change what an agent believes; only authority can change what the workflow means. An implementation detail, environment marker, reproduced failure, convenient workaround, or internal mechanism has no policy meaning unless explicit human instruction, repository canon, or the owning skill or contract establishes that meaning.

Before introducing a new workflow mode, semantic branch, exception, flag, validation ceremony, or constraint, name the authority that requires it and the existing contract it preserves or explicitly changes. A reproduced failure authorizes investigation and repair, not a new policy. When no authority requires changed semantics, preserve the existing workflow semantics and fix the failure within them.

## Reversible-work autonomy

An instruction to do work authorizes the reversible investigation, diagnosis, repair, focused verification, and already-authorized publication preparation needed to produce the requested result. Do not convert implementation uncertainty into a permission question when inspection or a reversible technical ruling can resolve it.

## Human stop boundary

Ask the human when requirements, product/canon choices, or authority remain human-owned, or before an unauthorized destructive, irreversible, permission-changing, security-sensitive, or externally consequential action. Routine validation, diagnosis, and repair are not approval boundaries.

## Bounded reading

Classify the request before broad bootstrap work. Load only the owning doctrine and references required by the selected route. Every read must have a purpose that can change the next lawful action; stop reading when that action is known.

## Evidence and model neutrality

Record claims against observable source, tool, test, and publication evidence. Do not infer model capability, hidden reasoning mode, or completion from absence of evidence. The contract is behaviorally identical across model families; model selection is a separate capability decision.
