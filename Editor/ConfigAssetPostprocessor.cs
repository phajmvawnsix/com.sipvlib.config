using System;
using System.Collections.Generic;
using UnityEditor;

namespace SiPVLib.Config.Editor
{
    /// <summary>
    /// Watches for GameConfig assets being added/removed/moved under the four configured root
    /// folders and refreshes only the affected <see cref="ConfigLocation"/> roots, instead of
    /// requiring a manual "Update Config Root" click. Debounced via <see cref="EditorApplication.delayCall"/>
    /// so a burst of imports (e.g. a git pull bringing in many new configs at once) triggers one
    /// refresh pass rather than one per asset.
    /// </summary>
    public class ConfigAssetPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<ConfigLocation> DirtyLocations = new();
        private static bool _refreshScheduled;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var settings = MasterWindowSettings.instance;

            MarkDirtyIfConfigured(importedAssets, settings);
            MarkDirtyIfConfigured(deletedAssets, settings);
            MarkDirtyIfConfigured(movedAssets, settings);
            MarkDirtyIfConfigured(movedFromAssetPaths, settings);

            if (DirtyLocations.Count == 0 || _refreshScheduled) return;

            _refreshScheduled = true;
            EditorApplication.delayCall += ProcessDirtyLocations;
        }

        private static void MarkDirtyIfConfigured(string[] paths, MasterWindowSettings settings)
        {
            foreach (var path in paths)
            {
                var location = ResolveLocation(path, settings);
                if (location.HasValue)
                {
                    DirtyLocations.Add(location.Value);
                }
            }
        }

        private static ConfigLocation? ResolveLocation(string path, MasterWindowSettings settings)
        {
            foreach (ConfigLocation location in Enum.GetValues(typeof(ConfigLocation)))
            {
                var folder = settings.GetRootFolder(location);
                if (!string.IsNullOrEmpty(folder) && path.StartsWith(folder + "/", StringComparison.Ordinal))
                {
                    return location;
                }
            }

            return null;
        }

        private static void ProcessDirtyLocations()
        {
            _refreshScheduled = false;

            foreach (var location in DirtyLocations)
            {
                ConfigRootEditorSync.RefreshLocation(location);
            }
            DirtyLocations.Clear();

            ConfigRootRefsEditor.UpdateCache();
        }
    }
}
