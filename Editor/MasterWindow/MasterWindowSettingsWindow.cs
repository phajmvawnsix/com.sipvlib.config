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
        private bool _onlyCheckRootFolders;

        private MasterWindowSettings _settings;

        public static void ShowWindow()
        {
            var window = GetWindow<MasterWindowSettingsWindow>("Master Window Settings");
            const float width = 600f, height = 400f;
            window.position = new Rect(
                (Screen.currentResolution.width - width) / 2f,
                (Screen.currentResolution.height - height) / 2f,
                width, height);
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
            _onlyCheckRootFolders = _settings.onlyCheckRootFolders;
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

            _onlyCheckRootFolders = EditorGUILayout.ToggleLeft(
                new GUIContent("Only Check Root Folders",
                    "When on, config discovery is scoped to each location's root folder. When off, the whole Assets folder is scanned and configs are matched by their own declared Store Location."),
                _onlyCheckRootFolders);

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
            _onlyCheckRootFolders = true;
        }

        private void SaveSettings()
        {
            _settings.rootFolderLocal = _rootFolderLocal;
            _settings.rootFolderResources = _rootFolderResources;
            _settings.rootFolderAddressable = _rootFolderAddressable;
            _settings.rootFolderRemoteConfig = _rootFolderRemoteConfig;
            _settings.onlyCheckRootFolders = _onlyCheckRootFolders;
            _settings.SaveSettings();
        }
    }
}