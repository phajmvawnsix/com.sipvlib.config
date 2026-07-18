using System;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor
{
    public class ConfigRefAttributeDrawer : OdinAttributeDrawer<ConfigRefAttribute, string>
    {
        private GUIStyle _wrapLabelStyle;

        private void EnsureStyles()
        {
            _wrapLabelStyle ??= new GUIStyle(SirenixGUIStyles.Label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = true
            };
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var refAttribute = Attribute;
            var entry = ValueEntry;
            var id = entry.SmartValue;
            GameConfig currentItem = null;
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    currentItem = ConfigRootRefsEditor.GetConfig<GameConfig>(id);
                }
                catch (Exception e)
                {
                    CustomLog.LogError($"Error retrieving GameConfig with Id '{id}': {e.Message}");
                }
            }

            var expectedBaseType = refAttribute.TypeConstraint ?? typeof(GameConfig);

            if (refAttribute.PreviewOnly)
            {
                DrawPreviewOnlyLayout(label, refAttribute, entry, currentItem, expectedBaseType);
                return;
            }

            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginHorizontalToolbar();
            if (label != null)
            {
                EnsureStyles();
                const float labelWidth = 100f;
                var labelHeight = _wrapLabelStyle.CalcHeight(label, labelWidth);
                var labelRect = GUILayoutUtility.GetRect(labelWidth, labelHeight, _wrapLabelStyle,
                    GUILayout.Width(labelWidth));
                GUI.Label(labelRect, label, _wrapLabelStyle);
            }

            Texture previewTex = null;
            if (currentItem != null)
            {
                try
                {
                    previewTex = currentItem.GetEditorIcon();
                }
                catch (Exception e)
                {
                    CustomLog.LogError($"Error getting preview icon for GameConfig with Id '{id}': {e.Message}");
                }
            }

            var previewRect = GUILayoutUtility.GetRect(50f, 50f, GUILayout.Width(50f));
            var picked =
                SirenixEditorFields.UnityPreviewObjectField(previewRect, currentItem, previewTex, expectedBaseType);
            var newObj = picked as GameConfig;
            if (newObj != null && refAttribute.TypeConstraint != null)
            {
                var newType = newObj.GetType();
                var constraint = refAttribute.TypeConstraint;
                var valid = constraint == newType ||
                            (refAttribute.AllowInherited && constraint.IsAssignableFrom(newType));
                if (!valid) newObj = currentItem;
            }

            if (newObj != currentItem)
            {
                entry.SmartValue = newObj ? newObj.Id : string.Empty;
                GUI.changed = true;
                id = entry.SmartValue;
                currentItem = newObj;
            }

            const float idMinWidth = 100f;
            EditorGUI.BeginChangeCheck();
            var newId = GUILayout.TextField(id ?? string.Empty, GUILayout.MinWidth(idMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                if (newId != id)
                {
                    entry.SmartValue = newId;
                    GUI.changed = true;
                }
            }

            if (currentItem != null && GUILayout.Button("Select", GUILayout.Width(50f)))
            {
                Selection.activeObject = currentItem;
                EditorGUIUtility.PingObject(currentItem);
            }

            if (!string.IsNullOrEmpty(entry.SmartValue) && currentItem == null)
            {
                SirenixEditorGUI.ErrorMessageBox($"Config with Id '{entry.SmartValue}' not found.");
            }

            GUILayout.FlexibleSpace();
            SirenixEditorGUI.EndHorizontalToolbar();
            var lastRect = GUILayoutUtility.GetLastRect();
            var dropAreaRect = new Rect(lastRect.x, lastRect.y - 40, lastRect.width, 40);
            HandleDragAndDrop(dropAreaRect, refAttribute, entry, currentItem);
            SirenixEditorGUI.EndBox();
        }

        private static void DrawPreviewOnlyLayout(GUIContent label, ConfigRefAttribute attr, IPropertyValueEntry<string> entry,
            GameConfig currentItem, Type expectedBaseType)
        {
            Texture previewTex = null;
            if (currentItem != null)
            {
                try
                {
                    previewTex = currentItem.GetEditorIcon();
                }
                catch (Exception e)
                {
                    CustomLog.LogError(
                        $"Error getting preview icon for GameConfig with Id '{entry.SmartValue}': {e.Message}");
                }
            }

            GUILayout.BeginHorizontal();

            if (label != null)
            {
                GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth));
            }

            var previewRect = GUILayoutUtility.GetRect(50f, 50f, GUILayout.Width(50f), GUILayout.Height(50f));
            var picked =
                SirenixEditorFields.UnityPreviewObjectField(previewRect, currentItem, previewTex, expectedBaseType);

            var newObj = picked as GameConfig;
            if (newObj != null && attr.TypeConstraint != null)
            {
                var newType = newObj.GetType();
                var constraint = attr.TypeConstraint;
                var valid = constraint == newType || (attr.AllowInherited && constraint.IsAssignableFrom(newType));
                if (!valid) newObj = currentItem;
            }

            if (newObj != currentItem)
            {
                entry.SmartValue = newObj ? newObj.Id : string.Empty;
                GUI.changed = true;
            }

            if (!string.IsNullOrEmpty(entry.SmartValue) && currentItem == null)
            {
                var errorIcon = EditorGUIUtility.IconContent("console.erroricon");
                var errorRect = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.Height(16f));
                GUI.Label(errorRect,
                    new GUIContent(errorIcon.image, $"Config with Id '{entry.SmartValue}' not found."));
            }

            if (currentItem != null && GUILayout.Button("Select", GUILayout.Width(50f)))
            {
                Selection.activeObject = currentItem;
                EditorGUIUtility.PingObject(currentItem);
            }

            GUILayout.EndHorizontal();

            var layoutRect = GUILayoutUtility.GetLastRect();
            HandleDragAndDrop(layoutRect, attr, entry, currentItem);
        }

        private static void HandleDragAndDrop(Rect dropRect, ConfigRefAttribute attr, IPropertyValueEntry<string> entry,
            GameConfig currentItem)
        {
            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition))
            {
                CustomLog.Log("Mouse is outside the drop area. Ignoring drag event.");
                return;
            }

            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            {
                CustomLog.Log("Drag event is not allowed.");
                return;
            }

            var valid = false;
            GameConfig candidate = null;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                candidate = obj as GameConfig;
                if (candidate == null)
                    continue;

                if (attr.TypeConstraint == null)
                {
                    valid = true;
                    break;
                }

                var constraint = attr.TypeConstraint;
                var cType = candidate.GetType();
                if (constraint != cType && (!attr.AllowInherited || !constraint.IsAssignableFrom(cType)))
                    continue;

                valid = true;
                break;
            }

            if (!valid)
            {
                CustomLog.LogWarning("Invalid object type dragged. Expected type: " +
                                 (attr.TypeConstraint != null ? attr.TypeConstraint.Name : "GameConfig"));
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (candidate != null && candidate != currentItem)
                {
                    entry.SmartValue = candidate.Id;
                    GUI.changed = true;
                }
            }

            evt.Use();
        }
    }
}