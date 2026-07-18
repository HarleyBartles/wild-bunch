# Script entrypoint contract

## Read when

Read when creating or backfilling an agent-facing script, especially one that
must run in multiple shells or invoke mutation behavior.

## Contract

Put core behavior in one portable owner, normally Python. Provide real Bash
and PowerShell entrypoints with equivalent arguments, exit behavior, and
observable results; wrappers translate shell syntax only and do not become
competing implementations. Make behavior deterministic, support --check for
safe inspection where meaningful, and test the core plus each entrypoint.

Do not make an installer prune authored local skills, local guides, or other
declared source custody. Installation and projection are runtime concerns;
authored content remains in its canonical repository home. Scripts must not
import installed skill trees or user caches as source.

Before a script writes, follow
[mutation-script-safety.md](mutation-script-safety.md). Keep temporary
outputs in external scratch custody and let repository-local policy supply
repository-specific paths, commands, CI, and exceptions.
