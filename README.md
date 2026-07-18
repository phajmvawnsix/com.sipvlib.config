# com.sipvlib.config

Part of [SiPVLib](https://github.com/phajmvawnsix/SiPVLib). A ScriptableObject-based game config system (`GameConfig`/`ConfigManager`/`ConfigRoot`) supporting Local/Resources/Addressable/RemoteConfig-backed data sources, with an optional Odin Inspector-powered editor tool (MasterWindow) for authoring configs.

## Install

Add to your project's `Packages/manifest.json`:

```json
"com.sipvlib.config": "https://github.com/phajmvawnsix/com.sipvlib.config.git",
"com.sipvlib.debugging": "https://github.com/phajmvawnsix/com.sipvlib.debugging.git",
"com.sipvlib.event": "https://github.com/phajmvawnsix/com.sipvlib.event.git",
"com.sipvlib.utilities": "https://github.com/phajmvawnsix/com.sipvlib.utilities.git",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
"com.gameworkstore.googleprotobufunity": "https://github.com/GameWorkstore/google-protobuf-unity.git#3.15.2012"
```

UPM does not automatically resolve nested git dependencies — you must add the `com.sipvlib.*`, UniTask, and protobuf entries above yourself alongside this package. `com.unity.addressables`, `com.unity.services.core`, `com.unity.services.authentication`, and `com.unity.remote-config-runtime` resolve automatically from Unity's package registry.

## Optional: Odin Inspector

This package integrates with [Odin Inspector](https://odininspector.com) (Sirenix) if you have it installed, but does NOT require it and does NOT bundle it — Odin is a paid Unity Asset Store asset and cannot be redistributed here.

- **Without Odin installed**: Config's runtime classes (`GameConfig`, `ConfigRoot`, etc.) work fully with plain Unity Inspector rendering — no custom attributes, grouping, or validation UI. The `MasterWindow` editor tool for authoring configs is unavailable (its assembly won't compile without Odin).
- **With Odin installed** (purchase + import from the Asset Store, which auto-defines the `ODIN_INSPECTOR` scripting define symbol): Config's runtime classes light up Odin's inspector attributes (grouping, conditional visibility, validation), and the `MasterWindow` editor tool becomes available.

No manual setup is needed beyond installing Odin itself — detection is automatic via the `ODIN_INSPECTOR` define.

## Optional: Firebase RemoteConfig

`FirebaseRemoteConfigProvider` (one of two `IRemoteConfigProvider` implementations, alongside `UnityRemoteConfigProvider`) requires the Firebase RemoteConfig SDK and does NOT bundle it. It's gated behind the `FIREBASE_REMOTE_CONFIG` scripting define symbol, which is **not** auto-defined by the Firebase SDK — add it yourself in Player Settings > Scripting Define Symbols after importing Firebase RemoteConfig.

Without the define, `FirebaseRemoteConfigProvider` compiles as a stub (logs a warning, returns empty/default values) instead of failing the build.

## Documentation
- [Architecture](ARCHITECTURE.md)
- [Usage guide](USAGE.md) — original module documentation carried over from the SiPVLib monolith
