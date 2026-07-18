# Config Module — Architecture

## Folder map

```
Config/                        [SiPV.Config.asmdef — runtime, no editor-platform restriction]
├── ConfigLocation.cs           enum: Local | Resources | Addressable | RemoteConfig
├── ConfigManager.cs            runtime facade (MonoSingleton), Get<T>/GetAll<T> per location
├── ConfigRoot.cs                base ScriptableObject: Id/type-indexed lookup for one location
├── ConfigRootAddressable.cs    ConfigRoot + Addressables bootstrap
├── ConfigRootRemoteConfig.cs   ConfigRoot + RemoteConfigManager bootstrap
├── ConfigRootEditorSync.cs     #if UNITY_EDITOR: folder-scoped rebuild/duplicate-Id check, shared
│                                by GameConfig's live Inspector hooks and the Editor-asmdef watcher
├── MasterWindowSettings.cs     #if UNITY_EDITOR: the 4 configured root folders (ScriptableSingleton)
├── ConfigEvents.cs             ConfigLocationInitializedEvent / ConfigFullyInitializedEvent payloads
├── ConfigRefAttribute.cs       [ConfigRef] marker attribute (runtime-safe, no editor deps)
├── DurationAttribute.cs        marker for `long` fields = relative duration (drives a duration picker)
├── TimeAttribute.cs            marker for `long` fields = absolute Unix timestamp (drives a date picker)
├── Configs/                    GameConfig base + concrete config types
├── Compare/                    generic value-compare primitives (CompareMode, GameValueCompare)
├── GameConditions/              declarative condition system (GameCondition, GameConditionGroup)
├── RemoteConfig/                IRemoteConfigProvider strategy pattern + Firebase/Unity impls
└── Editor/                     [SiPV.Config.Editor.asmdef — Editor-only]
    ├── ConfigRootRefsEditor.cs      static Id/type -> GameConfig cache (scoped to root folders)
    ├── ConfigAssetPostprocessor.cs  watches root folders, debounces, calls ConfigRootEditorSync
    ├── ConfigRefAttribute.cs        [ConfigRef] drawer (drag & drop, preview, Id resolution)
    ├── ScriptableObjectCreator.cs   generic "create ScriptableObject subtype" searchable dialog
    └── MasterWindow/                custom Odin OdinMenuEditorWindow (browse/create/rename/...)
```

`Config/` and `Config/Editor/` used to be a single unrestricted assembly (a latent player-build
risk — see REVIEW.md). They're now two assemblies; Editor-only code can reference the runtime
assembly but not vice versa, which is why the shared auto-update logic (`ConfigRootEditorSync`) and
its folder configuration (`MasterWindowSettings`) live in the runtime assembly instead of `Editor/`
— `GameConfig`'s live Inspector hooks (§ below) need to call them directly.

## Class hierarchy

```
ScriptableObject
├── ConfigRoot
│   ├── ConfigRootAddressable      (overrides Init(): waits for Addressables.InitializeAsync())
│   └── ConfigRootRemoteConfig     (overrides Init(): awaits RemoteConfigManager.Instance.Init())
├── GameConfig (SerializedScriptableObject, Odin)
│   ├── EditorConfig               (always IgnoreInBuild = true)
│   ├── PrefabConfig                (+ GameObject prefab ref)
│   ├── SpriteConfig                (+ single/random sprite selection)
│   ├── SubGameConfig               (abstract marker base)
│   ├── SingletonConfig<T>          (CRTP: static Instance, editor-time AssetDatabase lookup)
│   └── ConfigInventoryItem         (rich inventory item: icon, type flags, amounts, cooldowns)
│   Compare/ConfigCustomComparer (abstract GameConfig)
│   └── ConfigCustomComparer<TValue> : IComparer<TValue>   (pluggable structured-value comparer)
└── GameConditionGroup             (AND/OR/XOR condition tree with nesting/parentheses)

MonoSingleton<T> (SiPV.Utilities)
├── ConfigManager
└── RemoteConfigManager
```

Every concrete config type funnels through `Configs.GameConfig`. Cross-references between configs
(and from `GameCondition`/`GameValueCompare`/`GameItem`) are never direct object refs at the data
level — they're `[ConfigRef(typeof(X))] string` Ids, resolved lazily via `ConfigRoot.GetConfig`
(runtime) or `ConfigRootRefsEditor.GetConfig<T>` (editor).

## Loading flow, per `ConfigLocation`

| Location | Bootstrap | Root asset | Entry point |
|---|---|---|---|
| **Local** | none — always available | `ConfigRoot` | `ConfigManager.Awake()` → `_configLocal.Init()` (fire-and-forget) |
| **Resources** | `Resources.LoadAsync<ConfigRoot>(path)` | `ConfigRoot` | `ConfigManager.InitResources()` (opt-in) |
| **Addressable** | `AssetReference.LoadAssetAsync<ConfigRoot>()` (the manager) + `Addressables.InitializeAsync()` (the root, with retry) | `ConfigRootAddressable` | `ConfigManager.InitAddressable()` (opt-in) |
| **RemoteConfig** | `RemoteConfigManager.Instance.Init()` (selects/inits provider, fetches) | `ConfigRootRemoteConfig` | `ConfigManager.InitRemoteConfig()` (opt-in) — also pushes fetched JSON into each config via `GameConfig.SetRemoteConfig` |

Each `ConfigRoot.Init()` (after its subclass-specific bootstrap) matches `_configsId` against
`_configsRef` by `Id`, builds `_configsLoaded` (Id → config) and `_configsLoadedByType`
(Type → config[]), and sets `IsInitialized = true`.

`GameConfig.SetRemoteConfig(string json)` uses `JsonUtility.FromJsonOverwrite(json, this)` —
reflection-based, by design, so `GameConfig` has zero compile-time dependency on any remote config
SDK. This is the seam between `ConfigManager.InitRemoteConfig()` and the actual provider data.

## Provider pattern (RemoteConfig only)

```
IRemoteConfigProvider
├── FirebaseRemoteConfigProvider   (Firebase SDK; retries Init() via OtherUtils.GetRetryTime)
└── UnityRemoteConfigProvider      (Unity Gaming Services; no retry/backoff — see REVIEW.md)

RemoteConfigManager : MonoSingleton<RemoteConfigManager>
  [SerializeField] RemoteConfigProvider currentRemoteConfigProvider   // Inspector-selectable enum
  private IRemoteConfigProvider _provider;                            // instantiated in Init() per enum value
```

This is the module's one clean Strategy pattern: swapping remote config backend is a single enum
change, zero code edits elsewhere. `RemoteConfigManager` is a thin facade — every public method
guards on `_provider == null` and delegates.

### Provider-pattern inconsistency

Local/Resources/Addressable have **no equivalent interface** — they're differentiated by
`ConfigLocation` + `ConfigRoot` subclassing (`ConfigRootAddressable` overrides `Init()` directly)
plus a parallel `switch (location)` in `ConfigManager.Get`/`GetAll` and in `ConfigRootRefsEditor`/
`MasterWindow.Core.cs`. Adding a 5th location today means touching 4+ switch statements instead of
implementing one interface. Not urgent — the current 4 locations are unlikely to grow — but if a
5th source shows up, consider unifying under an `IConfigLocationProvider`-style abstraction
mirroring `IRemoteConfigProvider`. See REVIEW.md for the concrete suggestion.

## Auto-update flow (editor)

Two independent triggers keep each location's `ConfigRoot` in sync without a manual click, both
funneling into the same `ConfigRootEditorSync.RefreshLocation(location)` (folder-scoped: finds/
creates the root, rescans just that location's configured folder, calls `ConfigRoot.UpdateConfigs`):

```
File added/removed/moved under a root folder
        │
        ▼
ConfigAssetPostprocessor.OnPostprocessAllAssets()   (Editor asmdef)
        │  maps path -> ConfigLocation via MasterWindowSettings, debounces via EditorApplication.delayCall
        ▼
ConfigRootEditorSync.RefreshLocation(location)      (runtime asmdef, cross-assembly call — legal direction)
        + ConfigRootRefsEditor.UpdateCache()         (Editor asmdef, refreshes the ConfigRef Id cache)

Id / StoreLocation / IgnoreInBuild edited in Inspector
        │
        ▼
GameConfig's Odin [OnValueChanged] hook               (runtime asmdef — same assembly, direct call)
        ▼
ConfigRootEditorSync.RefreshLocation(location)
```

`GameConfig` also has a `[ValidateInput]` hook on `_id` (`ConfigRootEditorSync.HasDuplicateId`) for
an immediate red warning on a colliding Id, independent of the refresh cycle.

## `ConfigRef` resolution flow (editor)

```
[ConfigRef(typeof(T))] public string someId;
        │
        ▼
ConfigRefAttributeDrawer.DrawPropertyLayout()   (Editor/ConfigRefAttribute.cs)
        │  resolves current value via
        ▼
ConfigRootRefsEditor.GetConfig<GameConfig>(id)   (root Config/, static cache)
        │  cache miss →
        ▼
ConfigRootRefsEditor.UpdateCache()   scans AssetDatabase.FindAssets("t:GameConfig") project-wide
```

`ConfigRootRefsEditor` also exposes `LocalRoot`/`ResourcesRoot`/`AddressableRoot`/`RemoteConfigRoot`
(one `ConfigRoot` asset per location, found via `AssetDatabase.FindAssets("t:ConfigRoot")`) —
used by editor tooling that needs the root asset itself rather than an individual config.

## `GameCondition` / `GameConditionGroup` evaluation flow

`GameCondition` (partial class) declares a `GameConditionType` (`UserData` / `LimitedTime` /
`ViewActive` / `UIActive`) plus type-specific parameters — currently only `UserData` has dedicated
fields (`GameCondition.UserData.cs`): either a raw `_userDataKey` or an inventory item reference
(`[ConfigRef(typeof(ConfigInventoryItem))] _inventoryItemId`), compared via a `GameValueCompare`
(type-tagged union: Integer/FloatingPoint/String/Bool/Structured, with `CompareMode` and, for
`Structured`, a `[ConfigRef(typeof(ConfigCustomComparer))] comparerId` + raw `valueJson`).

`GameConditionGroup` holds an ordered `GameConditionGroupData[]` (each pairing a `GameCondition`
with a `ConditionMergeType` — And/Or/Xor — and a `nestingLevel` for parenthesized sub-groups).
Evaluation is fully decoupled from any concrete gameplay-state system: callers pass a
`Func<GameCondition, bool> conditionEvaluator` (intended to be something like
`UserDataManager.IsConditionMet`, which lives outside this module). Two fast paths
(`isAndConditions`/`isOrConditions`) skip merge-type logic entirely for the common case; the
general case recursively evaluates by nesting level (`EvaluateAtNestingLevel`).

## Module dependencies

- **`SiPV.Utilities`** — `MonoSingleton<T>` (base of `ConfigManager`, `RemoteConfigManager`),
  `OtherUtils.GetRetryTime` (retry backoff in `ConfigRootAddressable` and
  `FirebaseRemoteConfigProvider` — but not `UnityRemoteConfigProvider`, see REVIEW.md),
  `Extensions.DeepClone<T>` (editor-only, `ConfigRoot.GetAllIds`).
- **`SiPV.Debugging`** — `CustomLog` for all logging, consistently used module-wide (no direct
  `Debug.Log` calls found).
- **`SiPV.Event`** — `ConfigManager` broadcasts two type-keyed events (`ConfigEvents.cs`):
  `ConfigLocationInitializedEvent { Location, Success }` after each location's init attempt
  (Local/Resources/Addressable/RemoteConfig, success or failure), and `ConfigFullyInitializedEvent`
  once, the first time `IsFullInitialized` becomes true. Lets other systems (loading screens,
  retry UI) react without polling `ConfigManager.Instance.IsFullInitialized` or holding a direct
  reference. `GameCondition`/`GameConditionGroup` evaluation still uses direct delegate injection
  (`Func<GameCondition, bool>`) rather than the event bus — that's a different concern (querying
  current state on demand vs. reacting to a one-time lifecycle transition) and doesn't need to change.
- **Third-party**: Odin Inspector/Serializer (pervasive — `SerializedScriptableObject`, `ShowIf`,
  `BoxGroup`, editor drawers), UniTask (all async), Unity Addressables, Firebase SDK / Unity Gaming
  Services (RemoteConfig providers only).
