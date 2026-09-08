# Changelog

## [1.3.0] - 2026-09-08

Odin Inspector is no longer required anywhere in this package -- replaced by Alchemy (com.annulusgames.alchemy, MIT). GameConfig drops SerializedScriptableObject for plain ScriptableObject. MasterWindow is rewritten off OdinMenuEditorWindow/OdinMenuTree onto a standard UnityEditor.IMGUI.Controls.TreeView (MasterWindowTreeView), keeping search, drag-out to [ConfigRef] fields, create/rename/duplicate/delete, and both view modes (Hierarchical / Root Folders). ConfigRefAttributeDrawer and ScriptableObjectCreator's type picker are rewritten as plain PropertyDrawer/EditorWindow. ConfigRefAttribute now derives from UnityEngine.PropertyAttribute (required for CustomPropertyDrawer resolution).

GameConfig field visibility fixes: _remoteConfigKey now only shows when StoreLocation is RemoteConfig (previously shown for every location -- noise for Local/Resources/Addressable). _storeLocation's hide condition and GetEditorIcon() now check the virtual IgnoreInBuild property instead of the raw field, so a subclass override (e.g. EditorConfig, always excluded from build) is respected instead of silently ignored.

## [1.2.3] - 2026-09-03

Pin com.sipvlib.debugging/event/utilities and com.cysharp.unitask to semver versions
(1.1.1/1.1.2/2.0.2/2.5.11) instead of git URLs. OpenUPM's registry rejects a package whose
dependencies field contains a raw git URL — this is what caused "Unable to add package
com.sipvlib.config: ... Version 'git+...' is invalid" when installing via the OpenUPM registry.

## [1.2.2] - 2026-09-03

Lower minimum Unity Editor version to 2022.3 LTS (was 6000.3) and add a `repository`
field to `package.json`, both required for OpenUPM registry submission.

Pin `com.unity.addressables` to 1.21.19 (was 2.9.1): Addressables 2.x only ships for Unity 6.x, so
the 2.9.1 pin made this package uninstallable on 2022.3 regardless of the `unity` field. 1.21.19 is
the version 2022.3 LTS itself ships. Package Manager resolves the *highest* version requested across
a project, so a Unity 6 project that also depends on Addressables 2.x directly still gets 2.x — this
pin only sets the floor.

## [1.2.1] - 2026-09-02

Editor performance fixes:

- `ConfigRefAttributeDrawer.HandleDragAndDrop` logged via `CustomLog` on its early-out paths, which
  are hit on **every repaint of every `[ConfigRef]` field** — each call captured a Unity stack trace
  and flooded the console. The event-type check now runs before the rect test and both early-outs
  are silent.
- `ConfigRootEditorSync.HasDuplicateId` backs an Odin `ValidateInput` on `GameConfig._id`, so it runs
  on every keystroke in the Id field, but it looped all four `ConfigLocation`s doing a separate
  `FindAssets` + `LoadAssetAtPath` sweep per location with no early exit — and with
  `onlyCheckRootFolders` disabled that became four identical whole-project scans. Now one scan,
  returning at the first match.

## [1.2.0] - 2026-09-02

Add `MasterWindowSettings.onlyCheckRootFolders` (default `true`): when disabled, `ConfigRootEditorSync`
discovers configs for a `ConfigLocation` by scanning the whole `Assets` folder and matching each
`GameConfig`'s own `StoreLocation`, instead of scoping the search to that location's configured root
folder. Toggle exposed in Master Window Settings.

Fix `MasterWindow` re-subscribing `Selection.selectionChanged` on every `ForceMenuTreeRebuild()`
(refresh button, menu-tree rebuilds), stacking duplicate handlers and firing `OnSelection` multiple
times per selection change the longer the window stays open.

## [1.1.0] - 2026-08-31

Add `AssetConfig.LoadAssetOnStartup`: when enabled, the asset is preloaded during that config's
`ConfigLocation` initialization (`ConfigManager.InitXXX` / `ConfigRoot.Init`) instead of lazily on
first access. Disabled by default; asset still loads on demand via `GetAsset()` (sync) or
`GetAssetAsync()` (async) either way. Also fixes `GetAssetAsync()` not caching the loaded
Addressable/Resources asset, which would have made preloading pointless.

## [1.0.2] - 2026-07-28

Fix Initialize config root bugs

## [1.0.1] - 2026-07-28

Fix Initialize bug

## [1.0.0] - 2026-07-18

Initial extraction from SiPVLib monolith.
