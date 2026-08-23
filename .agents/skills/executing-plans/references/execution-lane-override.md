# Execution Lane Override

Use when the execution skill you are in differs from the `Execution Strategy` written in the plan.

## Authority order

When picking an execution lane, follow this order:

1. **Human explicit direction.** The user told you which skill to use, or explicitly chose a lane (for example, by typing `/executing-plans`). This wins over all other signals.
2. **Your own assessment of the plan.** If the user did not direct the lane, read the plan, look at the task shape, and choose the lane that fits best.
3. **The plan's `Execution Strategy`.** This is a recommendation, not a command. It is one input to your decision.

## One-lane rule

Pick one lane before execution starts. Announce it:

> "I am using `<lane-name>` to execute this plan."

Then see it through. Do not switch lanes mid-execution unless the human asks you to.

## When the plan recommends a different lane

If the plan's `Execution Strategy` does not match the lane you chose, do not ask the human for confirmation by default. Instead:

1. Note the mismatch:
   - "Plan recommends `subagent-driven-development`. I am using `executing-plans` because you invoked it."
   - "Plan recommends `executing-plans`. I am using `subagent-driven-development` because the tasks are independent and this is the better fit."
2. Confirm to yourself that you have human direction or a defensible assessment for the mismatch.
3. Proceed. If you can give neither human direction nor a clear assessment, raise a focused question to the human.
