#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using UnityEditor;

namespace SiPVLib.Config
{
    /// <summary>
    /// Editor-only, runtime-assembly-resident sync logic shared between in-Inspector edits on
    /// <see cref="GameConfig"/> (which can't reference the Editor-only assembly) and the Editor
    /// tooling (Master Window, asset-change watcher) that lives in <c>SiPV.Config.Editor</c>.
    /// Every operation is scoped to a single configured root folder, so it stays cheap enough to
    /// run on every relevant edit instead of requiring a manual, project-wide rescan.
    /// </summary>
    public static class ConfigRootEditorSync
    {
        /// <summary>
        /// Finds (or creates) the <see cref="ConfigRoot"/> asset for <paramref name="location"/> and
        /// rebuilds it from the current <see cref="GameConfig"/> assets in that location's configured
        /// root folder.
        /// </summary>
        public static void RefreshLocation(ConfigLocation location)
        {
            var root = GetOrCreateRoot(location);
            var items = FindConfigsInLocation(location).Where(c => !c.IgnoreInBuild).ToArray();

            root.UpdateConfigs(location, items);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Cheap, folder-scoped check for whether another <see cref="GameConfig"/> — in any of the
        /// four configured root folders — already uses <paramref name="id"/>. Ids are treated as
        /// globally unique (matching how <see cref="ConfigManager.Get{T}"/>'s
        /// <c>findAllIfNotFound</c> and the Editor's Id cache both resolve across locations), so
        /// this checks every location, not just the config's own. Used for live Inspector validation.
        /// </summary>
        public static bool HasDuplicateId(string id, GameConfig self)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            foreach (ConfigLocation location in Enum.GetValues(typeof(ConfigLocation)))
            {
                foreach (var config in FindConfigsInLocation(location))
                {
                    if (config != self && config.Id == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Finds (or creates, saving to disk) the <see cref="ConfigRoot"/> asset for a location.</summary>
        public static ConfigRoot GetOrCreateRoot(ConfigLocation location)
        {
            var folder = MasterWindowSettings.instance.GetRootFolder(location);

            foreach (var guid in AssetDatabase.FindAssets("t:ConfigRoot", new[] { folder }))
            {
                var root = AssetDatabase.LoadAssetAtPath<ConfigRoot>(AssetDatabase.GUIDToAssetPath(guid));
                if (root != null && root.Location == location)
                {
                    return root;
                }
            }

            var newRoot = UnityEngine.ScriptableObject.CreateInstance<ConfigRoot>();
            newRoot.name = $"ConfigRoot_{location}";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var assetPath = Path.Combine(folder, $"{newRoot.name}.asset").Replace("\\", "/");
            AssetDatabase.CreateAsset(newRoot, assetPath);
            AssetDatabase.SaveAssets();
            CustomLog.Log($"[ConfigRootEditorSync] Created new ConfigRoot at {assetPath}");
            return newRoot;
        }

        private static List<GameConfig> FindConfigsInLocation(ConfigLocation location)
        {
            var folder = MasterWindowSettings.instance.GetRootFolder(location);
            var result = new List<GameConfig>();

            foreach (var guid in AssetDatabase.FindAssets("t:GameConfig", new[] { folder }))
            {
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null)
                {
                    result.Add(config);
                }
            }

            return result;
        }
    }
}
#endif
