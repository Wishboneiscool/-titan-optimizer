# Optimization policy

## Evidence tiers

- **Tier 1 — Strongly supported:** conservative, well-understood, measurable, and reversible.
- **Tier 2 — Context-dependent:** potentially useful for a detected workload or hardware state; never presented as universal.
- **Tier 3 — Experimental:** explicit warning, opt-in, and no automatic application.
- **Tier 4 — Deprecated/questionable:** retained only for research or migration analysis.
- **Tier 5 — Rejected:** excluded from the catalog used by the application.

## Automatic application gates

An optimization may be considered for automatic application only when it is reversible, not high or critical risk, compatible with the current Windows build, supported by a detection result, free of unresolved conflicts, and represented by a verified rollback procedure.

## Claims policy

The application must not claim FPS, latency, boot-time, or battery improvements without repeated measurements under comparable conditions. If variance overlaps the measured change, the result is reported as inconclusive.

## Security prohibitions

The project will not silently disable antivirus, firewall protections, updates, mitigations, or other security controls. It will not execute arbitrary downloaded scripts or hide changes from the user.
