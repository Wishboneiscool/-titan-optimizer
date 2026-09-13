# TITAN OPTIMIZER

Evidence-driven Windows performance optimization with measurement, safe change application, verification, and rollback.

> This project deliberately rejects placebo tweaks, silent system changes, and one-click application of unverified settings.

## Current status

**Phase 1 foundation plus functional desktop shell**

The repository now contains a Windows desktop app that can scan installed power plans, preview a selected change, perform a user-confirmed apply, verify the resulting state, record the change in SQLite, and roll back the last change.

```text
Detect → Validate → Preview → Confirm → Apply → Verify → Record → Roll back
```

The power-plan operation is intentionally classified as **Tier 2 — Context-dependent**. It does not promise better FPS, lower latency, or higher benchmark scores merely because a power plan changed.

## Safety principles

- Dry-run and preview before mutation.
- Explicit confirmation before applying a system change.
- No arbitrary command execution.
- The Windows adapter allowlists `powercfg.exe` and validates plan identifiers.
- The original active power-plan GUID is the rollback value.
- Verification is required after apply and rollback.
- Change history is stored locally in SQLite.
- Unsupported platforms fail closed.
- Experimental and questionable optimizations are never automatic defaults.

## Technology

- C# / .NET 8
- WPF desktop shell
- SQLite persistence through Microsoft.Data.Sqlite
- xUnit tests
- GitHub Actions on Windows runners

## Repository layout

```text
src/
  TitanOptimizer.App/        Functional WPF desktop shell
  TitanOptimizer.Core/       Shared contracts and optimization metadata
  TitanOptimizer.Persistence/SQLite change journal
  TitanOptimizer.Windows/    Windows-specific adapters

tests/
  TitanOptimizer.Windows.Tests/

docs/
  architecture.md
  optimization-policy.md

.github/workflows/
  build.yml
```

## First working slice

The app's first real optimization is a user-selected active power-plan change. It is useful as a safety-pipeline slice because the active GUID is detectable, the target must already exist, the original value is a clear rollback value, and the result can be verified by reading the active plan again.

The app does not automatically choose a power plan and does not claim that High performance is universally better.

## Roadmap

1. Functional power-plan desktop workflow — implemented
2. Persistent snapshots and richer audit records — next
3. Privileged broker and administrator detection
4. Benchmark association and variance reporting
5. Optimization definition loading and conflict resolution
6. Profiles and settings
7. Diagnostics and system inventory
8. Additional evidence-backed optimization primitives
9. Packaging, signing, installation, and release QA

## Explicit non-goals

This project will not include registry cleaning, automatic service disabling, arbitrary downloaded scripts, silent security weakening, fake system scores, or claims of universal performance improvements.
