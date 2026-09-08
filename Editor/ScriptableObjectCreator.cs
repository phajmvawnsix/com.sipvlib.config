using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Config.Editor
{
    /// <summary>
    /// Shows a searchable popup of every concrete <typeparamref name="T"/> subtype in the project,
    /// then a save-file dialog for the chosen type. Used by MasterWindow's "Create" menu item.
    /// </summary>
    public static class ScriptableObjectCreator
    {
        public static void ShowDialog<T>(string folder, Action<T> onSuccess = null) where T : ScriptableObject
        {
            var types = TypeCache.GetTypesDerivedFrom<T>()
                .Where(t => !t.IsAbstract)
                .OrderBy(GetMenuPath, StringComparer.Ordinal)
                .ToArray();

            if (types.Length == 0)
            {
                EditorUtility.DisplayDialog("No Types Found", $"No concrete subclasses of {typeof(T).Name} found in the project.", "OK");
                return;
            }

            if (types.Length == 1)
            {
                CreateAndSave(types[0], folder, onSuccess);
                return;
            }

            TypeSelectorWindow.Show(types, GetMenuPath, type => CreateAndSave(type, folder, onSuccess));
        }

        /// <summary>Groups by <see cref="ConfigCategoryAttribute"/> if present, otherwise flat.</summary>
        private static string GetMenuPath(Type type)
        {
            var category = type.GetCustomAttribute<ConfigCategoryAttribute>()?.Category;
            return string.IsNullOrEmpty(category) ? type.Name : $"{category}/{type.Name}";
        }

        private static void CreateAndSave<T>(Type type, string folder, Action<T> onSuccess) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance(type) as T;
            if (instance == null) return;

            var destination = folder.TrimEnd('/');

            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
                AssetDatabase.Refresh();
            }

            destination = EditorUtility.SaveFilePanel("Save as", destination, type.Name, "asset");

            if (string.IsNullOrEmpty(destination))
            {
                Object.DestroyImmediate(instance);
                return;
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var relative = Path.GetFullPath(destination).Replace(Path.GetFullPath(projectRoot!) + Path.DirectorySeparatorChar, "").Replace('\\', '/');

            if (relative.StartsWith("Assets/") || relative.StartsWith("Packages/"))
            {
                AssetDatabase.CreateAsset(instance, relative);
                AssetDatabase.Refresh();
                onSuccess?.Invoke(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
                EditorUtility.DisplayDialog("Invalid Path", "Assets must be created inside the Assets or Packages folder.", "OK");
            }
        }

        /// <summary>Minimal searchable type picker, standing in for Odin's OdinSelector.</summary>
        private class TypeSelectorWindow : EditorWindow
        {
            private Type[] _types;
            private string[] _labels;
            private Action<Type> _onSelected;
            private SearchField _searchField;
            private string _search = "";
            private Vector2 _scroll;

            public static void Show(Type[] types, Func<Type, string> labelSelector, Action<Type> onSelected)
            {
                var window = CreateInstance<TypeSelectorWindow>();
                window.titleContent = new GUIContent("Select Type");
                window._types = types;
                window._labels = types.Select(labelSelector).ToArray();
                window._onSelected = onSelected;
                window._searchField = new SearchField();

                var size = new Vector2(400, 400);
                window.position = new Rect(
                    (Screen.currentResolution.width - size.x) / 2f,
                    (Screen.currentResolution.height - size.y) / 2f,
                    size.x, size.y);
                window.ShowUtility();
            }

            private void OnGUI()
            {
                _search = _searchField.OnGUI(_search);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (var i = 0; i < _types.Length; i++)
                {
                    if (!string.IsNullOrEmpty(_search) &&
                        _labels[i].IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (GUILayout.Button(_labels[i], EditorStyles.label))
                    {
                        var picked = _types[i];
                        Close();
                        _onSelected?.Invoke(picked);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
