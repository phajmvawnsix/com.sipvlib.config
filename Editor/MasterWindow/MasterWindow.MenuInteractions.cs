using System.IO;
using SiPVLib.Config.Configs;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow
    {
        private void ShowFolderContextMenu(MasterWindowTreeItem folderItem)
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

        private void ShowMultiSelectionContextMenu(GameConfig[] configs)
        {
            var menu = new GenericMenu();

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate Selected"), false, () =>
            {
                foreach (var config in configs)
                {
                    DuplicateConfig(config);
                }
            });
            menu.AddItem(new GUIContent("Delete Selected"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("Delete Config",
                        $"Are you sure you want to delete selected {configs.Length} items?", "Delete", "Cancel")) return;

                foreach (var config in configs)
                {
                    DeleteConfig(config, false);
                }

                ForceMenuTreeRebuild();
            });

            menu.ShowAsContext();
        }

        private void CreateConfig(MasterWindowTreeItem folderItem)
        {
            var folderPath = GetFolderPath(folderItem);
            ScriptableObjectCreator.ShowDialog<GameConfig>(folderPath, TrySelectConfig);
        }

        private void RenameConfig(GameConfig config)
        {
            var newName = EditorUtility.SaveFilePanel("Rename Config", Path.GetDirectoryName(AssetDatabase.GetAssetPath(config)), config.name, "asset");
            if (string.IsNullOrEmpty(newName)) return;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var relativePath = Path.GetFullPath(newName).Replace(Path.GetFullPath(projectRoot!) + Path.DirectorySeparatorChar, "").Replace('\\', '/');

            if (relativePath.StartsWith("Assets/") || relativePath.StartsWith("Packages/"))
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
                    TrySelectConfig(config);
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
                TrySelectConfig(newGameConfig);
            }
        }

        private static void DeleteConfig(GameConfig config, bool askConfirmation = true)
        {
            if (askConfirmation && !EditorUtility.DisplayDialog("Delete Config",
                    $"Are you sure you want to delete '{config.name}'?", "Delete", "Cancel")) return;

            var assetPath = AssetDatabase.GetAssetPath(config);
            AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Selection.activeObject == config)
            {
                Selection.activeObject = null;
            }
        }
    }
}
