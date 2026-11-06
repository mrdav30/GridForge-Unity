# Changelog

## 10.0.0

- Require Unity 6.3 LTS (6000.3) or newer; CI validates Unity 6000.3.9f1.
- Bundle the released GridForge v10.0.0 .NET Standard 2.1 assemblies for both
  Standard and Lean, with matching FixedMathSharp-Unity and
  SwiftCollections-Unity v8.0.0 dependencies.
- Migrate the Unity logger and inspector to `Chronicler.Diagnostics` shared
  types. Rebuild scripts and assemblies that use the logger's diagnostic APIs.
- Align CI with the lower-stack Unity packages: Ubuntu 24.04, account-based
  Unity activation, and sample-import Git trust through the runner global config.
- Document the v9 to v10 upgrade and renamed upstream collection APIs.
- Fix the export script's case-sensitive output argument so `-OutputPath`
  reaches the Unity exporter.

## 9.0.0

- Renamed `GridTracerTests` to `GridTraceVisualizer` and refined trace
  visualization APIs.
- Replaced arbitrary global `GridWorld` lookup with explicit assigned, local,
  then parent resolution.
- Added intentional `TryApplyBlockage()` APIs for blocker updates.
- Published the hosted Unity guide and API reference.
- Aligned the standard and lean Unity packages while preserving their distinct
  dependency chains.
