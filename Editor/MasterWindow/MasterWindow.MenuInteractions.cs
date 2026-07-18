using System.IO;
using System.Linq;
using SiPVLib.Config.Configs;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow
    {
        private void AddDragHandles(OdinMenuItem item)
        {
            item.OnDrawItem += _ => HandleMenuItemDraw(item);
        }
        
        private void HandleFolderInteractions(OdinMenuItem item)
        {
            var currentEvent = Event.current;

            if (!item.Rect.Contains(currentEvent.mousePosition)) return;
            switch (currentEvent.type)
            {
                case EventType.MouseDown when currentEvent.button == 1:
                    ShowFolderContextMenu(item);
                    currentEvent.Use();
                    break;
                case EventType.MouseDown when currentEvent.button == 0 &&
                                              currentEvent.clickCount == 2:
                    item.Toggled = !item.Toggled;
                    currentEvent.Use();
                    GUI.changed = true;
                    break;
            }
        }

        private void HandleConfigInteractions(GameConfig config, Rect rect)
        {
            var currentEvent = Event.current;

            if (!rect.Contains(currentEvent.mousePosition) || currentEvent.type != EventType.MouseDown ||
                currentEvent.button != 1) return;
            
            var selectedConfigs = MenuTree.Selection.ToArray();
            if (selectedConfigs.Length > 1)
            {
                ShowMultiSelectionContextMenu(selectedConfigs);
            }
            else
            {
                ShowConfigContextMenu(config);
            }

            currentEvent.Use();
        }
        
        private void ShowFolderContextMenu(OdinMenuItem folderItem)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Config"), false, () => CreateConfig(folderItem));
            menu.ShowAsContext();
        }

        private void ShowConfigContextMenu(GameConfig config)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Select in Project"), false, () =>
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Rename"), false, () => RenameConfig(config));
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateConfig(config));
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteConfig(config));

            menu.ShowAsContext();
        }

        private void ShowMultiSelectionContextMenu(OdinMenuItem[] items)
        {
            var menu = new GenericMenu();
    
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate Selected"), false, () =>
            {
                foreach (var item in items)
                {
                    if (item.Value is GameConfig config)
                    {
                        DuplicateConfig(config);
                    }
                }
            });
            menu.AddItem(new GUIContent("Delete Selected"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("Delete Config",
                        $"Are you sure you want to delete selected {items.Length} items?", "Delete", "Cancel")) return;
                foreach (var item in items)
                {
                    if (item.Value is GameConfig config)
                    {
                        DeleteConfig(config, false);
                    }
                }
            });

            menu.ShowAsContext();
        }

        private void CreateConfig(OdinMenuItem item)
        {
            if (!IsFolder(item)) return;

            var folderPath = GetFolderPath(item);
            ScriptableObjectCreator.ShowDialog<GameConfig>(folderPath, TrySelectMenuItemWithObject);
        }
        
        private void RenameConfig(GameConfig config)
        {
            var newName = EditorUtility.SaveFilePanel("Rename Config", Path.GetDirectoryName(AssetDatabase.GetAssetPath(config)), config.name, "asset");
            if (string.IsNullOrEmpty(newName)) return;
            
            if (PathUtilities.TryMakeRelative(Path.GetDirectoryName(Application.dataPath), newName, out var relativePath))
            {
                var assetPath = AssetDatabase.GetAssetPath(config);
                var error = AssetDatabase.RenameAsset(assetPath, Path.GetFileNameWithoutExtension(relativePath));
                if (!string.IsNullOrEmpty(error))
                {
                    EditorUtility.DisplayDialog("Rename Failed", $"Failed to rename asset: {error}", "OK");
                }
                else
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    TrySelectMenuItemWithObject(config);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "The selected path is invalid. Please select a path within the Assets folder.", "OK");
            }
        }

        private void DuplicateConfig(GameConfig config)
        {
            var assetPath = AssetDatabase.GetAssetPath(config);
            var folder = Path.GetDirectoryName(assetPath);
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var extension = Path.GetExtension(assetPath);
            
            var filePath = AssetDatabase.GenerateUniqueAssetPath(string.IsNullOrWhiteSpace(folder) ?
                $"{fileName}_Copy{extension}" :
                Path.Combine(folder, $"{fileName}_Copy{extension}"));
            AssetDatabase.CopyAsset(assetPath, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            var newGameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(filePath);
            if (newGameConfig != null)
            {
                TrySelectMenuItemWithObject(newGameConfig);
            }
        }

        private static void DeleteConfig(GameConfig config, bool askConfirmation = true)
        {
            if (askConfirmation)
            {
                if (!EditorUtility.DisplayDialog("Delete Config",
                        $"Are you sure you want to delete '{config.name}'?", "Delete", "Cancel")) return;
                
                var assetPath = AssetDatabase.GetAssetPath(config);
                AssetDatabase.DeleteAsset(assetPath);
            }
            else
            {
                var assetPath = AssetDatabase.GetAssetPath(config);
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Selection.activeObject == config)
            {
                Selection.activeObject = null;
            }
        }
    }
}