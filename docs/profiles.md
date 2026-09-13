# Profiles

Profiles are versioned JSON documents containing optimization IDs, enabled state, and typed string parameters. They are data only; they cannot contain executable commands.

The initial built-in profile is `safe`. It contains no automatic changes. Future profiles may include evidence-backed, reversible optimizations after compatibility and rollback checks pass.

Profile loading validates the schema version, identifier, and name before the profile can be used. Writes are atomic through a temporary file followed by replacement.
