# SiPV.Config

Centralizes all serializable game configuration (`GameConfig` ScriptableObjects) behind one
runtime facade (`ConfigManager`), editable through a custom Odin **Master Window**, and loadable
from four interchangeable sources — **Local**, **Resources**, **Addressable**, **RemoteConfig** —
selected per-config without any code change.

Depends on `SiPV.Utilities`, `SiPV.Debugging`, and (as of this pass) `SiPV.Event` for
initialization broadcasts.

See [ARCHITECTURE.md](ARCHITECTURE.md) for structure/flow, [REVIEW.md](REVIEW.md) for the findings
and suggestions from this review pass.

---

## Getting a config at runtime

```csharp
using SiPVLib.Config;
using SiPVLib.Config.Configs;

// Look up by Id in a specific location (default Local)
var item = ConfigManager.Get<ConfigInventoryItem>("sword_01");

// Search all four locations if not found in the given one
var item = ConfigManager.Get<ConfigInventoryItem>("sword_01", ConfigLocation.Local, findAllIfNotFound: true);

// All configs of a type, across all locations
var allItems = ConfigManager.GetAll<ConfigInventoryItem>();

// All configs of a type, from one location only
var localItems = ConfigManager.GetAll<ConfigInventoryItem>(ConfigLocation.Local);

// TryGet — no warning logged if not found, for call sites that handle absence themselves
if (ConfigManager.TryGet<ConfigInventoryItem>("sword_01", out var sword))
{
    Equip(sword);
}
```

`ConfigManager` is a `MonoSingleton<ConfigManager>` (from `SiPV.Utilities`) — it must exist in the
scene (or be marked `CreateIfNotFound`) before these calls run. `Get`/`GetAll` are static
convenience wrappers around the singleton `Instance`.

### Initialization order

- **Local** is initialized automatically in `Awake()` — always available once the singleton wakes.
- **Resources**, **Addressable**, **RemoteConfig** are opt-in and async — call once (e.g. at boot):

```csharp
await ConfigManager.InitResources();
await ConfigManager.InitAddressable();
await ConfigManager.InitRemoteConfig();
```

Check `ConfigManager.Instance.IsFullInitialized` if you need to gate on all four being ready, or
listen for the broadcast instead of polling:

```csharp
using SiPVLib.Event;

// Fires once per location (Local/Resources/Addressable/RemoteConfig), success or failure
this.ListenEvent<ConfigLocationInitializedEvent>(evt =>
{
    if (!evt.Success) ShowRetryUi(evt.Location);
});

// Fires once, the first time every location has finished its init attempt
this.ListenEvent<ConfigFullyInitializedEvent>(_ => HideLoadingScreen());
```

---

## Defining a new config type

1. Subclass `GameConfig` (or a more specific base like `PrefabConfig`, `SpriteConfig`,
   `SubGameConfig`) in `SiPVLib.Config.Configs` (or your own namespace).
2. Add an `[CreateAssetMenu]` if you want it creatable from the project menu, or rely on the
   Master Window's "+" button (uses `ScriptableObjectCreator`, which lists every non-abstract
   subtype of `GameConfig` in a searchable picker). Add `[ConfigCategory("Items")]` on the class to
   group it under a named category in that picker instead of the flat alphabetical list.
3. Set `Id`, `ConfigName`, and `StoreLocation` on the asset (Master Window enforces the folder
   convention per location via `GameConfig.ValidateLocation`).
4. Reference the config elsewhere by Id using `[ConfigRef(typeof(YourConfigType))]` on a `string`
   field — this gets a rich drag-and-drop/preview drawer in the Inspector for free:

```csharp
[ConfigRef(typeof(ConfigInventoryItem))]
[SerializeField] private string _rewardItemId;
```

`ConfigRefAttribute` itself has no editor dependency (safe in runtime assemblies); the drawer
(`Editor/ConfigRefAttribute.cs` → `ConfigRefAttributeDrawer`) resolves the Id via
`ConfigRootRefsEditor.GetConfig<T>` at edit time only.

---

## Switching / adding a remote config provider

`RemoteConfigManager` (a `MonoSingleton`) picks its `IRemoteConfigProvider` implementation from an
Inspector-exposed enum — **no code change needed to switch provider**:

```csharp
public enum RemoteConfigProvider { FirebaseRemoteConfig, UnityRemoteConfig }
```

To add a new remote source, implement `IRemoteConfigProvider`:

```csharp
public interface IRemoteConfigProvider
{
    UniTask Init();
    UniTask FetchConfigs();
    string GetJson(string key);
    T GetValue<T>(string key);
}
```

and add a case to `RemoteConfigManager.Init()`'s switch. See `FirebaseRemoteConfigProvider` /
`UnityRemoteConfigProvider` for reference implementations.

> Local/Resources/Addressable do **not** go through this provider interface — they're handled by
> `ConfigRoot` subclassing instead (`ConfigRootAddressable`, `ConfigRootRemoteConfig`). See
> [ARCHITECTURE.md](ARCHITECTURE.md#provider-pattern-inconsistency) for the gap this leaves.

---

## Using the Master Window

`SiPV/Master Window` menu item opens a custom Odin `OdinMenuEditorWindow` for browsing and
authoring every `GameConfig` asset in the project, grouped by `ConfigLocation`:

- **Create**: "+" toolbar button or right-click a folder → "Create Config" (searchable type picker).
- **Rename / Duplicate / Delete**: right-click a config (single or multi-selection supported).
- **Update Config Root**: toolbar button — manual full rebuild of all four `ConfigRoot`s, still
  useful after bulk changes made outside the Editor (e.g. editing files directly). Day-to-day this
  is now automatic — see below.
- **Validate All**: toolbar button — scans all four locations, logs every invalid config's reason
  to the Console, selects them all in the Project window, and shows a summary dialog. Use this to
  catch problems before they ship instead of discovering them one at a time by selecting each asset.
- **Settings** (gear icon): configure the four root folders (Local/Resources/Addressable/RemoteConfig)
  and menu tree view style (Hierarchical vs. flat-with-folders); persisted project-wide in
  `ProjectSettings/MasterWindowSettings.asset`.

Invalid configs (failing `GetInvalidReason()`/`ValidateLocation`) show a red error icon/label in
the tree and inspector. A `[ConfigRef]` field whose Id resolves to a config shows a **Select**
button next to the preview to jump straight to it in the Project window.

## Auto-updating ConfigRoot

`ConfigRoot`s now stay in sync automatically, scoped to the four configured root folders (assets
placed outside them are not auto-discovered — keep configs inside their location's folder):

- **Add / remove / move** a `GameConfig` asset (in the Project window, or an external tool/git
  pull) → `ConfigAssetPostprocessor` (an `AssetPostprocessor`) detects it and refreshes the
  affected location's `ConfigRoot`. A burst of changes (e.g. pulling many new configs at once) is
  debounced into a single refresh pass, not one per file.
- **Edit a config's `Id` / `StoreLocation` / `IgnoreInBuild` in the Inspector** → refreshes
  immediately via an Odin `OnValueChanged` hook, no need to wait for a reimport or click anything.
- **Duplicate `Id`** — checked project-wide across all four locations — shows a red validation box
  in the Inspector as soon as you type it (`ValidateInput` on the `Id` field) — no need to run
  "Update Config Root" to discover the collision.

This logic lives in `ConfigRootEditorSync` (runtime assembly, `#if UNITY_EDITOR`-guarded — see
[ARCHITECTURE.md](ARCHITECTURE.md) for why it can't live in the Editor assembly) and
`Editor/ConfigAssetPostprocessor.cs`.
