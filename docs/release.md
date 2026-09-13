# Release process

The supported first release artifact is a self-contained Windows x64 publish.

From a Windows development machine with the .NET 8 SDK installed:

```powershell
./build/package.ps1
```

The script publishes `TitanOptimizer.App` with:

- `win-x64` runtime
- self-contained deployment
- single-file output
- native library self-extraction
- symbols disabled for release output

It creates `artifacts/TitanOptimizer-win-x64.zip`.

This is not yet a signed installer. Code signing, clean-install verification, clean-uninstall verification, and upgrade testing are release gates before a public production release.
