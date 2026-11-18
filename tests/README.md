# Regression tests

On Windows, run from the repository folder:

```powershell
.\tests\run.ps1
```

Requires the .NET Framework C# compiler. No running WoW or game files are needed for the default suite. The runner compiles the production source together with the tests and executes the actual x86 helper bytes against synthetic character data in its own process.

Optional: supply your supported originals to also verify exact output hashes, unchanged inputs, LAA variants, and rejection of an unrelated byte change:

```powershell
.\tests\run.ps1 -OriginalPaths @("C:\Games\WoW\Wow.exe")
```

Checks include successful output creation, existing-file protection, bad-hash cleanup, rename-failure cleanup, malformed inputs, six targeted models with three native models, other-character exclusion, and unrelated-model pass-through. Disk exhaustion and abrupt termination are not simulated. These checks do not establish gameplay compatibility with druid forms, overlapping disguises, or high memory usage.
