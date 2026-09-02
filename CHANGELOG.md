# Changelog

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
