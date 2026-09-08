using System;
using System.Collections.Generic;
using System.Linq;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public partial class MasterWindow
    {
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Update Config Root", EditorStyles.toolbarButton))
            {
                UpdateConfigRoot();
            }

            if (GUILayout.Button("Validate All", EditorStyles.toolbarButton))
            {
                ValidateAllConfigs();
            }

            var refreshIcon = EditorGUIUtility.IconContent("Refresh");
            if (GUILayout.Button(refreshIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
            {
                ForceMenuTreeRebuild();
            }

            var viewTypeLabel = _treeViewType == MenuTreeViewType.Hierarchical ? "Hierarchical" : "Root Folders";
            if (GUILayout.Button(viewTypeLabel, EditorStyles.toolbarButton))
            {
                var newViewType = (MenuTreeViewType) (((int) _treeViewType + 1) % Enum.GetValues(typeof(MenuTreeViewType)).Length);
                if (newViewType == _treeViewType) return;

                _treeViewType = newViewType;
                MasterWindowSettings.instance.menuTreeViewType = newViewType;
                MasterWindowSettings.instance.SaveSettings();
                ForceMenuTreeRebuild();
            }

            if (GUILayout.Button("Create", EditorStyles.toolbarButton))
            {
                var selectedConfig = _treeView?.GetSelectedConfig();
                var targetFolder = selectedConfig != null
                    ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedConfig))?.Replace('\\', '/')
                    : _rootFolderLocal;

                ScriptableObjectCreator.ShowDialog<GameConfig>(string.IsNullOrEmpty(targetFolder) ? _rootFolderLocal : targetFolder, TrySelectConfig);
            }

            GUILayout.FlexibleSpace();

            var selected = _treeView?.GetSelectedConfig();
            if (selected != null)
            {
                var invalidReason = selected.GetInvalidReason();
                if (!string.IsNullOrWhiteSpace(invalidReason))
                {
                    var errorIcon = EditorGUIUtility.IconContent("console.erroricon");
                    GUILayout.Label(new GUIContent(errorIcon.image, invalidReason));
                }

                if (GUILayout.Button(selected.name, EditorStyles.toolbarButton))
                {
                    Selection.activeObject = selected;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                MasterWindowSettingsWindow.ShowWindow();
                EditorApplication.update += OnSettingsUpdate;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void TrySelectConfig(GameConfig config)
        {
            if (config == null) return;
            ForceMenuTreeRebuild();
            _treeView.SelectConfig(config);
            Selection.activeObject = config;
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
