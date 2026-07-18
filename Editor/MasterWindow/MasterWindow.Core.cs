using System.IO;
using System.Linq;
using SiPVLib.Config.Configs;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow : OdinMenuEditorWindow
    {
        private string _rootFolderLocal;
        private string _rootFolderResources;
        private string _rootFolderAddressable;
        private string _rootFolderRemoteConfig;
        
        private MenuTreeViewType _treeViewType = MenuTreeViewType.Hierarchical;
        
        [MenuItem("SiPV/Master Window")]
        private static void Open()
        {
            var window = GetWindow<MasterWindow>();
            window.titleContent = new GUIContent("Master Window");
            window.Show();
        }
        
        private new void OnEnable()
        {
            base.OnEnable();
            
            var settings = MasterWindowSettings.instance;
            _rootFolderLocal = settings.rootFolderLocal;
            _rootFolderResources = settings.rootFolderResources;
            _rootFolderAddressable = settings.rootFolderAddressable;
            _rootFolderRemoteConfig = settings.rootFolderRemoteConfig;
            _treeViewType = settings.menuTreeViewType;
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            // Listen for Selection.activeObject changes to update menu selection
            Selection.selectionChanged += OnSelection;
        }

        private void OnSelection()
        {
            if (Selection.activeObject == null)
                return;
            
            // Check if activeObject is a GameConfig
            if (Selection.activeObject is not GameConfig)
            {
                return;
            }

            var curWindowSelection = this.MenuTree.Selection.FirstOrDefault();
            if (curWindowSelection != null && ReferenceEquals(curWindowSelection.Value, Selection.activeObject))
                return; // Already selected in the window, no need to update
            
            // Try to find the selected object in the menu tree and select it
            var menuItem = this.MenuTree.EnumerateTree().FirstOrDefault(x => ReferenceEquals(x.Value, Selection.activeObject));
            if (menuItem == null) return;
            
            this.MenuTree.Selection.Clear();
            menuItem.Select();
        }

        private string GetFolderPath(OdinMenuItem folderItem)
        {
            var pathComponents = new System.Collections.Generic.List<string>();
            var current = folderItem;
            
            while (current != null)
            {
                pathComponents.Insert(0, current.Name);
                current = current.Parent;
            }
            
            var rootPath = "";
            if (pathComponents.Count > 0)
            {
                var firstComponent = pathComponents[0];
                if (firstComponent.Contains("Resources"))
                    rootPath = _rootFolderResources;
                else if (firstComponent.Contains("Addressable"))
                    rootPath = _rootFolderAddressable;
                else if (firstComponent.Contains("RemoteConfig"))
                    rootPath = _rootFolderRemoteConfig;
                else
                    rootPath = _rootFolderLocal;
            }
            
            return pathComponents.Count > 0 ? Path.Combine(rootPath, string.Join("/", pathComponents)).Replace("\\", "/") : rootPath;
        }

        private static ConfigRoot GetConfigs(ConfigLocation location)
        {
            return ConfigRootEditorSync.GetOrCreateRoot(location);
        }

        private static void UpdateConfigRoot()
        {
            foreach (var location in new[]
                     {
                         ConfigLocation.Resources,
                         ConfigLocation.Addressable,
                         ConfigLocation.RemoteConfig,
                         ConfigLocation.Local
                     })
            {
                ConfigRootEditorSync.RefreshLocation(location);
            }

            ConfigRootRefsEditor.UpdateCache();
        }
    }
}