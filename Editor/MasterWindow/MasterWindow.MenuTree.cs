using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiPVLib.Config.Configs;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow
    {
        protected override void DrawMenu()
        {
            if (this.MenuTree == null)
                return;
            
            SirenixEditorGUI.BeginHorizontalToolbar();

            var viewTypeIcon = _treeViewType switch
            {
                MenuTreeViewType.Hierarchical => EditorIcons.Tree.Active,
                MenuTreeViewType.FlatWithFolders => EditorIcons.List.Active,
                _ => throw new ArgumentOutOfRangeException()
            };
            var viewTypeTooltip = _treeViewType switch
            {
                MenuTreeViewType.Hierarchical => "Hierarchical View",
                MenuTreeViewType.FlatWithFolders => "Flat View with Folders",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (SirenixEditorGUI.ToolbarButton(new GUIContent(viewTypeIcon, viewTypeTooltip)))
            {
                var newViewType = (MenuTreeViewType)(((int)_treeViewType + 1) % Enum.GetValues(typeof(MenuTreeViewType)).Length);
            
                if (_treeViewType == newViewType) return;
            
                _treeViewType = newViewType;
                ForceMenuTreeRebuild();
            }

            switch (_treeViewType)
            {
                case MenuTreeViewType.Hierarchical:
                    GUILayout.Label("Hierarchical");
                    break;
                case MenuTreeViewType.FlatWithFolders:
                    GUILayout.Label("Root Folders");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            GUILayout.FlexibleSpace();
            
            // "+" button to create new config
            if (SirenixEditorGUI.ToolbarButton(new GUIContent(EditorIcons.Plus.Active, "Create New Config")))
            {
                var targetFolder = _rootFolderLocal; // Default to local folder
                var selectingItem = this.MenuTree.Selection.FirstOrDefault();
                if (selectingItem != null)
                {
                    if (IsFolder(selectingItem))
                    {
                        var folderPath = AssetDatabase.GetAssetPath(selectingItem.Value as Object);
                        if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath))
                        {
                            targetFolder = folderPath;
                        }
                    }
                }
                ScriptableObjectCreator.ShowDialog<GameConfig>(targetFolder, TrySelectMenuItemWithObject);
            }

            SirenixEditorGUI.EndHorizontalToolbar();
            
            base.DrawMenu();
        }
        
        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true)
            {
                DefaultMenuStyle =
                {
                    IconSize = 28.0f
                },
                Config =
                {
                    DrawSearchToolbar = true
                }
            };

            InitMenuTree(tree);

            EnhanceSearch(tree);

            ConfigRootRefsEditor.UpdateCache();
            return tree;
        }

        private void InitMenuTree(OdinMenuTree tree)
        {
            tree.MenuItems.Clear();

            // Ensure root folders exist and behave like folders even if empty
            // var rootNames = new[] { "Local", "Addressable", "Resources", "RemoteConfig" };
            // foreach (var name in rootNames)
            // {
            //     var rootItem = new OdinMenuItem(tree, name, null);
            //     tree.AddMenuItemAtPath("", rootItem);
            // }

            InitMenuTree(tree, _rootFolderLocal, "Local");
            InitMenuTree(tree, _rootFolderAddressable, "Addressable");
            InitMenuTree(tree, _rootFolderResources, "Resources");
            InitMenuTree(tree, _rootFolderRemoteConfig, "RemoteConfig");
            
            tree.EnumerateTree()
                .Where(IsConfig)
                .ForEach(AddDragHandles)
                .AddIcons<GameConfig>(x =>
                {
                    if (x == null || !x.IsValid()) return EditorIcons.UnityErrorIcon;

                    var icon = x.GetEditorIcon();
                    
                    if (!x.IgnoreInBuild) return icon;
                    
                    var alertCircleActive = EditorIcons.UnityWarningIcon;
                    return alertCircleActive;

                });
            tree.EnumerateTree()
                .Where(IsFolder)
                .ForEach(folderItem =>
                {
                    if (IsInvalid(folderItem))
                    {
                        folderItem.Icon = EditorIcons.UnityErrorIcon;
                    }
                    
                    // Listen for right-click events to show context menu for folders
                    folderItem.OnRightClick = HandleFolderRightClick;
                });

            tree.SortMenuItemsByName();
        }

        private void InitMenuTree(OdinMenuTree tree, string rootFolder, string menuName)
        {
            switch (_treeViewType)
            {
                case MenuTreeViewType.Hierarchical:
                    tree.AddAllAssetsAtPath(menuName, rootFolder, typeof(GameConfig), true);
                    break;
                case MenuTreeViewType.FlatWithFolders:
                    tree.AddAllAssetsAtPath(menuName, rootFolder, typeof(GameConfig), false, true);
                    var subFolders = AssetDatabase.GetSubFolders(rootFolder);
                    foreach (var folderPath in subFolders)
                    {
                        var folderName = Path.GetFileName(folderPath);
                        tree.AddAllAssetsAtPath(folderName, rootFolder, typeof(GameConfig), true, true);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsConfig(OdinMenuItem item)
        {
            return item != null && item.Value as GameConfig;
        }
        
        private static bool IsFolder(OdinMenuItem item)
        {
            return item.Value == null || (item.ChildMenuItems.Count > 0 && item.Value is not GameConfig);
        }
        
        private static void GetConfigs(OdinMenuItem item, ref List<GameConfig> result)
        {
            if (IsConfig(item))
            {
                var gameConfig = item.Value as GameConfig;
                result.Add(gameConfig);
                return;
            }
            
            if (!IsFolder(item)) return;
            
            foreach (var child in item.ChildMenuItems)
            {
                GetConfigs(child, ref result);
            }
        }
        private static GameConfig[] GetConfigs(OdinMenuItem item)
        {
            var result = new List<GameConfig>();
            GetConfigs(item, ref result);
            return result.ToArray();
        }

        private static bool IsInvalid(OdinMenuItem item)
        {
            var configs = GetConfigs(item);
            return configs.Any(configItem => !configItem.IsValid());
        }

        private static void EnhanceSearch(OdinMenuTree tree)
        {
            tree.EnumerateTree()
                .Where(IsConfig)
                .ForEach(item =>
                {
                    var config = item.Value as GameConfig;
                    if (config == null) return;

                    var searchString = new System.Text.StringBuilder();

                    searchString.Append(item.Name);
                    searchString.Append(" ");

                    if (!string.IsNullOrEmpty(config.Id))
                    {
                        searchString.Append(config.Id);
                        searchString.Append(" ");
                    }

                    var typeNames = GetTypeNames(config.GetType());
                    foreach (var typeName in typeNames)
                    {
                        searchString.Append(typeName);
                        searchString.Append(" ");
                    }

                    var assetPath = AssetDatabase.GetAssetPath(config);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(assetPath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            searchString.Append(fileName);
                        }
                    }

                    item.SearchString = searchString.ToString();
                });
        }
        
        private void HandleFolderRightClick(OdinMenuItem item)
        {
            ShowFolderContextMenu(item);
        }
        
        private void HandleMenuItemDraw(OdinMenuItem item)
        {
            var rect = item.Rect;
            DragAndDropUtilities.DragZone(rect, item.Value, false, false);

            if (IsFolder(item))
            {
                HandleFolderInteractions(item);
            }
            else if (IsConfig(item))
            {
                var config = item.Value as GameConfig;
                HandleConfigInteractions(config, rect);
            }
        }

        private static List<string> GetTypeNames(Type type)
        {
            var result = new List<string>();
            var currentType = type;
            
            while (currentType != null && currentType != typeof(object) && currentType != typeof(ScriptableObject))
            {
                result.Add(currentType.Name);
                currentType = currentType.BaseType;
            }
            
            return result;
        }
    }
}