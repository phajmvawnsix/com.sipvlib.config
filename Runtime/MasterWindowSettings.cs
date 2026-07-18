#if UNITY_EDITOR
using System;
using UnityEditor;

namespace SiPVLib.Config
{
    [Serializable]
    public enum MenuTreeViewType
    {
        Hierarchical,
        FlatWithFolders
    }

    /// <summary>
    /// Project-wide (not per-user) Master Window settings: the four configured root folders backing
    /// each <see cref="ConfigLocation"/>, used both by the Editor tooling and by the runtime-callable
    /// <see cref="ConfigRootEditorSync"/> to know where to look for/rebuild each location's configs.
    /// </summary>
    [FilePath("ProjectSettings/MasterWindowSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class MasterWindowSettings : ScriptableSingleton<MasterWindowSettings>
    {
        public string rootFolderLocal = "Assets/Master/Local";
        public string rootFolderResources = "Assets/Master/Resources";
        public string rootFolderAddressable = "Assets/Master/Addressable";
        public string rootFolderRemoteConfig = "Assets/Master/RemoteConfig";
        public MenuTreeViewType menuTreeViewType = MenuTreeViewType.Hierarchical;

        public string GetRootFolder(ConfigLocation location)
        {
            return location switch
            {
                ConfigLocation.Local => rootFolderLocal,
                ConfigLocation.Resources => rootFolderResources,
                ConfigLocation.Addressable => rootFolderAddressable,
                ConfigLocation.RemoteConfig => rootFolderRemoteConfig,
                _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
            };
        }

        public string[] AllRootFolders()
        {
            return new[] { rootFolderLocal, rootFolderResources, rootFolderAddressable, rootFolderRemoteConfig };
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }
}
#endif
