# Architecture

## Boundaries

The application is split into four trust boundaries:

1. **Desktop UI** — displays state and collects explicit user intent. It does not mutate Windows configuration.
2. **Core engine** — validates optimization definitions, resolves conflicts, creates plans, and records outcomes.
3. **Windows adapters** — expose narrowly scoped operations such as power-plan inspection. Each adapter fails closed on unsupported systems.
4. **Privileged broker** — planned component for operations that require elevation. It will accept typed, allowlisted requests over authenticated local IPC rather than arbitrary commands.

## Change lifecycle

```text
Detect → Validate → Preview → Snapshot → Apply → Verify → Record → Benchmark → Keep/Roll back
```

A change is not successful merely because a command returned exit code zero. The resulting Windows state must be read back and compared with the requested state.

## Current slice

`PowerPlanChangeService` demonstrates preview, dry-run, apply, verification, automatic rollback after failed verification, and explicit rollback. `PowerCfgPowerPlanProvider` is deliberately narrow: it can invoke only the built-in `powercfg.exe` adapter with structured, internally generated arguments.

## Next boundaries to add

- SQLite-backed snapshots and audit records
- Broker authorization and administrator detection
- Benchmark association and variance reporting
- UI preview and change-history workflows
- Definition loading with schema and signature validation
