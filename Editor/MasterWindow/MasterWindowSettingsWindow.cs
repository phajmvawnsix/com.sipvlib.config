using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor.MasterWindow
{
    public class MasterWindowSettingsWindow : EditorWindow  
    {
        private const string DefaultLocalRoot = "Assets/Master/Local";
        private const string DefaultResourcesRoot = "Assets/Master/Resources";
        private const string DefaultAddressableRoot = "Assets/Master/Addressable";
        private const string DefaultRemoteConfigRoot = "Assets/Master/RemoteConfig";

        private string _rootFolderLocal;
        private string _rootFolderResources;
        private string _rootFolderAddressable;
        private string _rootFolderRemoteConfig;

        private MasterWindowSettings _settings;

        public static void ShowWindow()
        {
            var window = GetWindow<MasterWindowSettingsWindow>("Master Window Settings");
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(600, 400);
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = MasterWindowSettings.instance;
            
            _rootFolderLocal = string.IsNullOrEmpty(_settings.rootFolderLocal) ? DefaultLocalRoot : _settings.rootFolderLocal;
            _rootFolderResources = string.IsNullOrEmpty(_settings.rootFolderResources) ? DefaultResourcesRoot : _settings.rootFolderResources;
            _rootFolderAddressable = string.IsNullOrEmpty(_settings.rootFolderAddressable) ? DefaultAddressableRoot : _settings.rootFolderAddressable;
            _rootFolderRemoteConfig = string.IsNullOrEmpty(_settings.rootFolderRemoteConfig) ? DefaultRemoteConfigRoot : _settings.rootFolderRemoteConfig;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Master Window Folder Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Local Root Folder
            DrawFolderSelector("Local Root Folder", ref _rootFolderLocal, "Select Local Root Folder", "Folder for local config files");

            EditorGUILayout.Space();

            // Resources Root Folder
            DrawFolderSelector("Resources Root Folder", ref _rootFolderResources, "Select Resources Root Folder", "Folder for resources-based config files");

            EditorGUILayout.Space();

            // Addressable Root Folder
            DrawFolderSelector("Addressable Root Folder", ref _rootFolderAddressable, "Select Addressable Root Folder", "Folder for addressable config files");
            
            EditorGUILayout.Space();

            // Addressable Root Folder
            DrawFolderSelector("Remote Config Root Folder", ref _rootFolderRemoteConfig, "Select Remote Config Root Folder", "Folder for remote config files");

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Buttons
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Reset to Defaults"))
                {
                    ResetToDefaults();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }

                if (GUILayout.Button("Save"))
                {
                    SaveSettings();
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private static void DrawFolderSelector(string label, ref string folderPath, string dialogTitle, string tooltip)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(150));
                
                EditorGUILayout.TextField(folderPath);
                
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel(dialogTitle, Application.dataPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.dataPath))
                        {
                            folderPath = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Folder", "The folder must be inside the Assets folder.", "OK");
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ResetToDefaults()
        {
            _rootFolderLocal = DefaultLocalRoot;
            _rootFolderResources = DefaultResourcesRoot;
            _rootFolderAddressable = DefaultAddressableRoot;
            _rootFolderRemoteConfig = DefaultRemoteConfigRoot;
        }

        private void SaveSettings()
        {
            _settings.rootFolderLocal = _rootFolderLocal;
            _settings.rootFolderResources = _rootFolderResources;
            _settings.rootFolderAddressable = _rootFolderAddressable;
            _settings.rootFolderRemoteConfig = _rootFolderRemoteConfig;
            _settings.SaveSettings();
        }
    }
}