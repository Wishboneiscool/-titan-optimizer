# TITAN OPTIMIZER

Evidence-driven Windows performance optimization with measurement, safe change application, verification, and rollback.

> This project deliberately rejects placebo tweaks, silent system changes, and one-click application of unverified settings.

## Current status

**Working MVP foundation — Phase 1 through initial diagnostics**

The repository contains a Windows desktop app that can:

- Scan basic system inventory: Windows version, logical processors, memory, and readable storage volumes.
- Inventory supported startup locations in read-only mode.
- Enumerate installed power plans.
- Preview a selected power-plan change.
- Require explicit confirmation before applying it.
- Verify the resulting state.
- Restore the prior power plan, including rollback context recovered from local history after restart.
- Persist change history and benchmark samples in SQLite.
- Run a deterministic local workload without claiming it represents gaming performance.
- Compare repeated benchmark samples using a variance-aware analyzer.
- Load versioned JSON optimization definitions and profiles.
- Enforce catalog, risk, reversibility, administrator, and confirmation gates.
- Publish a self-contained Windows x64 package through GitHub Actions.

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
- The desktop app uses an `asInvoker` manifest and does not silently elevate.

## Technology

- C# / .NET 8
- WPF desktop shell
- SQLite persistence through Microsoft.Data.Sqlite
- JSON catalog and profiles
- xUnit tests
- GitHub Actions on Windows runners
- Self-contained `win-x64` publish script

## Repository layout

```text
src/
  TitanOptimizer.App/        Functional WPF desktop shell
  TitanOptimizer.Core/       Shared contracts, diagnostics, safety, and benchmarking
  TitanOptimizer.Persistence/SQLite journals and JSON stores
  TitanOptimizer.Windows/    Windows-specific adapters

data/
  optimizations/             Versioned optimization definitions
  profiles/                  Built-in safe profile

tests/
  TitanOptimizer.Core.Tests/
  TitanOptimizer.Persistence.Tests/
  TitanOptimizer.Windows.Tests/

docs/
  architecture.md
  optimization-policy.md
  profiles.md
  release.md

build/
  package.ps1
```

## First working slice

The first real optimization is a user-selected active power-plan change. It is useful as a safety-pipeline slice because the active GUID is detectable, the target must already exist, the original value is a clear rollback value, and the result can be verified by reading the active plan again.

The app does not automatically choose a power plan and does not claim that High performance is universally better.

## Roadmap

1. Working desktop shell, safety pipeline, diagnostics, persistence, and initial benchmark — implemented
2. Privileged broker for operations that require elevation
3. Richer benchmark sessions with baseline/after labeling in the UI
4. Recommendation engine with bottleneck-aware applicability rules
5. Read-only service, network, driver, GPU, and thermal diagnostics
6. Safe startup action workflow with per-item backup and rollback
7. Profiles and settings editor
8. Additional evidence-backed optimization primitives
9. Code signing, installer, clean-install testing, and release QA

## Explicit non-goals

This project will not include registry cleaning, automatic service disabling, arbitrary downloaded scripts, silent security weakening, fake system scores, or claims of universal performance improvements.
