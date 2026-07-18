using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor
{
    public static class ScriptableObjectCreator
    {
        public static void ShowDialog<T>(string folder, Action<T> onSuccess = null)
            where T : ScriptableObject
        {
            var selector = new ScriptableObjectSelector<T>(folder, onSuccess);

            if (selector.SelectionTree.EnumerateTree().Count() == 1)
            {
                selector.SelectionTree.EnumerateTree().First().Select();
                selector.SelectionTree.Selection.ConfirmSelection();
            }
            else
            {
                var currentEvent = Event.current;
                if (currentEvent != null)
                {
                    selector.ShowInPopup(400);
                    currentEvent.Use();
                }
                else
                {
                    selector.ShowInPopup(new Rect(Screen.width / 2f - 200, Screen.height / 2f, 400, 0));
                }
            }
        }

        private class ScriptableObjectSelector<T> : OdinSelector<Type> where T : ScriptableObject
        {
            private readonly Action<T> _onSuccess;
            private readonly string _folder;

            public ScriptableObjectSelector(string folder, Action<T> onSuccess = null)
            {
                this._onSuccess = onSuccess;
                this._folder = folder;
                this.SelectionConfirmed += this.ShowSaveFileDialog;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                var scriptableObjectTypes = AssemblyUtilities.GetTypes(AssemblyCategory.ProjectSpecific)
                    .Where(x => x.IsClass && !x.IsAbstract && x.InheritsFrom(typeof(T)));

                tree.Selection.SupportsMultiSelect = false;
                tree.Config.DrawSearchToolbar = true;
                tree.AddRange(scriptableObjectTypes, GetMenuPath)
                    .AddThumbnailIcons();
            }

            /// <summary>Groups by <see cref="ConfigCategoryAttribute"/> if present, otherwise flat.</summary>
            private static string GetMenuPath(Type type)
            {
                var category = type.GetCustomAttribute<ConfigCategoryAttribute>()?.Category;
                return string.IsNullOrEmpty(category) ? type.GetNiceName() : $"{category}/{type.GetNiceName()}";
            }

            private void ShowSaveFileDialog(IEnumerable<Type> selection)
            {
                var instance = ScriptableObject.CreateInstance(selection.FirstOrDefault()) as T;

                if (instance == null) return;

                var destination = this._folder.TrimEnd('/');

                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                    AssetDatabase.Refresh();
                }

                destination = EditorUtility.SaveFilePanel("Save as", destination, instance.GetType().GetNiceName(), "asset");

                if (!string.IsNullOrEmpty(destination) && PathUtilities.TryMakeRelative(Path.GetDirectoryName(Application.dataPath), destination, out destination))
                {
                    if (destination.StartsWith("Assets/") || destination.StartsWith("Packages/"))
                    {
                        AssetDatabase.CreateAsset(instance, destination);
                        AssetDatabase.Refresh();

                        this._onSuccess?.Invoke(instance);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                        EditorUtility.DisplayDialog("Invalid Path", "Assets must be created inside the Assets or Packages folder.", "OK");
                    }
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }
    }
}