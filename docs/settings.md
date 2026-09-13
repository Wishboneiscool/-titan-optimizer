# Settings

Settings are versioned JSON data and never contain executable commands. The initial defaults are conservative:

- Safe profile selected.
- Recommendations may be shown, but are not automatically applied.
- Snapshots are enabled before apply workflows.
- High-risk restore-point behavior is enabled for future high-risk operations.
- Experimental definitions are hidden.
- Reduced motion is disabled until the UI adds animated transitions.
- Start with Windows is disabled.

Application settings stored under the user's local application-data directory are written atomically through a temporary file and replacement.
