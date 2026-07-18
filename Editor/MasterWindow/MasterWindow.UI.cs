using System.Collections.Generic;
using System.Linq;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow
    {
        protected override void OnBeginDrawEditors()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Update Config Root")))
            {
                UpdateConfigRoot();
            }

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Validate All")))
            {
                ValidateAllConfigs();
            }

            if (SirenixEditorGUI.ToolbarButton(new GUIContent(EditorIcons.Refresh.Active)))
            {
                ForceMenuTreeRebuild();
            }

            GUILayout.FlexibleSpace();

            var selected = MenuTree.Selection.FirstOrDefault();
            if (selected != null)
            {
                if (selected.Value is GameConfig configItem)
                {
                    var invalidReason = configItem.GetInvalidReason();
                    if (!string.IsNullOrWhiteSpace(invalidReason))
                    {
                        var richTextStyle = new GUIStyle(GUI.skin.label) { richText = true };
                        GUILayout.Label($"<color=red>{invalidReason}</color>", richTextStyle);

                        GUIHelper.PushColor(Color.red);
                        GUILayout.Label(new GUIContent(EditorIcons.UnityErrorIcon, invalidReason));
                        GUIHelper.PopColor();
                    }
                }

                if (selected.Value is Object obj)
                {
                    if (GUILayout.Button(obj.name))
                    {
                        Selection.activeObject = obj;
                    }
                }
                else
                {
                    GUILayout.Label(selected.Name);
                }
            }
            
            // Fill middle space
            GUILayout.FlexibleSpace();
            
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Settings")))
            {
                MasterWindowSettingsWindow.ShowWindow();

                EditorApplication.update += OnSettingsUpdate;
            }
            
            SirenixEditorGUI.EndHorizontalToolbar();
        }
        
        
        /// <summary>
        /// Scans every configured root folder for invalid configs (<see cref="GameConfig.IsValid"/>)
        /// and reports them in one place, instead of only surfacing a reason when a config happens
        /// to be selected.
        /// </summary>
        private static void ValidateAllConfigs()
        {
            var invalidConfigs = new List<GameConfig>();

            foreach (var folder in MasterWindowSettings.instance.AllRootFolders())
            {
                foreach (var guid in AssetDatabase.FindAssets("t:GameConfig", new[] { folder }))
                {
                    var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (config != null && !config.IsValid())
                    {
                        invalidConfigs.Add(config);
                    }
                }
            }

            if (invalidConfigs.Count == 0)
            {
                EditorUtility.DisplayDialog("Validate All Configs", "All configs are valid.", "OK");
                return;
            }

            foreach (var config in invalidConfigs)
            {
                CustomLog.LogWarning($"[MasterWindow] Invalid config '{config.name}' " +
                                      $"({AssetDatabase.GetAssetPath(config)}): {config.GetInvalidReason()}");
            }

            EditorUtility.DisplayDialog("Validate All Configs",
                $"{invalidConfigs.Count} invalid config(s) found. See Console for details.", "OK");

            Selection.objects = invalidConfigs.Cast<Object>().ToArray();
        }

        private void OnSettingsUpdate()
        {
            EditorApplication.update -= OnSettingsUpdate;
            
            var settings = MasterWindowSettings.instance;
            var isChanged = false;
            
            if (_rootFolderLocal != settings.rootFolderLocal)
            {
                _rootFolderLocal = settings.rootFolderLocal;
                isChanged = true;
            }

            if (_rootFolderResources != settings.rootFolderResources)
            {
                _rootFolderResources = settings.rootFolderResources;
                isChanged = true;
            }

            if (_rootFolderAddressable != settings.rootFolderAddressable)
            {
                _rootFolderAddressable = settings.rootFolderAddressable;
                isChanged = true;
            }

            if (_rootFolderRemoteConfig != settings.rootFolderRemoteConfig)
            {
                _rootFolderRemoteConfig = settings.rootFolderRemoteConfig;
                isChanged = true;
            }

            if (_treeViewType != settings.menuTreeViewType)
            {
                _treeViewType = settings.menuTreeViewType;
                isChanged = true;
            }   
            
            if (isChanged)
            {
                ForceMenuTreeRebuild(); 
            }
        }
    }
}