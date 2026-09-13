# TITAN OPTIMIZER

Evidence-driven Windows performance optimization with measurement, safe change application, verification, and rollback.

> This project deliberately rejects placebo tweaks, silent system changes, and one-click application of unverified settings.

## Status

**Phase 1 foundation / first vertical slice**

The repository currently contains the domain model and a Windows power-plan adapter designed to exercise the complete safe-change pipeline:

```text
Detect → Validate → Preview → Snapshot → Apply → Verify → Record → Roll back
```

The first implementation is intentionally context-dependent. It does not promise better FPS, lower latency, or higher benchmark scores merely because a power plan changed.

## Safety principles

- Dry-run and preview before mutation.
- No arbitrary command execution.
- The Windows adapter allowlists `powercfg.exe` and passes structured arguments.
- The original active power-plan GUID is the rollback value.
- Verification is required after apply and rollback.
- Unsupported platforms fail closed.
- Experimental and questionable optimizations are never automatic defaults.

## Technology

- C# / .NET 8
- WPF planned for the desktop UI
- MVVM planned for presentation logic
- SQLite planned for snapshots, history, profiles, and benchmark results
- xUnit for tests
- GitHub Actions for continuous integration

## Repository layout

```text
src/
  TitanOptimizer.Core/       Shared contracts and optimization metadata
  TitanOptimizer.Windows/    Windows-specific adapters

tests/
  TitanOptimizer.Windows.Tests/

docs/
  architecture.md
  optimization-policy.md

.github/workflows/
  build.yml
```

## First vertical slice

The first real optimization is a user-selected active power-plan change. It is classified as **Tier 2 — Context-dependent** because its effect depends on hardware, firmware, Windows configuration, workload, and battery policy.

Before adding more system changes, this slice must gain:

1. Persistent snapshots and audit records.
2. A desktop preview screen.
3. A privileged broker boundary.
4. Repeatable before/after benchmark association.
5. Failure-injection tests for partial apply and verification failure.

## Explicit non-goals

This project will not include registry cleaning, automatic service disabling, arbitrary downloaded scripts, silent security weakening, fake system scores, or claims of universal performance improvements.

## Roadmap

1. Core contracts and safe power-plan slice
2. Snapshot and audit persistence
3. Privileged broker and permission detection
4. Desktop shell and preview workflow
5. Benchmark engine
6. Additional evidence-backed optimization primitives
7. Profiles, diagnostics, packaging, and release QA
