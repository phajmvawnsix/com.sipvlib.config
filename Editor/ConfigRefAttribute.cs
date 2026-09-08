using System;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Config.Editor
{
    [CustomPropertyDrawer(typeof(ConfigRefAttribute))]
    public class ConfigRefAttributeDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const float RowHeight = 18f;
        private const float ButtonWidth = 50f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var refAttribute = (ConfigRefAttribute) attribute;
            var currentItem = ResolveCurrentItem(property.stringValue);

            var rows = refAttribute.PreviewOnly ? 1 : 2;
            if (!string.IsNullOrEmpty(property.stringValue) && currentItem == null) rows++;

            return rows * RowHeight + (rows - 1) * Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var refAttribute = (ConfigRefAttribute) attribute;
            var expectedBaseType = refAttribute.TypeConstraint ?? typeof(GameConfig);
            var id = property.stringValue;
            var currentItem = ResolveCurrentItem(id);

            var row = new Rect(position.x, position.y, position.width, RowHeight);

            var pickerRect = EditorGUI.PrefixLabel(row, label);
            var selectWidth = currentItem != null ? ButtonWidth + Spacing : 0f;
            var objectFieldRect = new Rect(pickerRect.x, pickerRect.y, pickerRect.width - selectWidth, pickerRect.height);
            var selectRect = new Rect(objectFieldRect.xMax + Spacing, pickerRect.y, ButtonWidth, pickerRect.height);

            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ObjectField(objectFieldRect, currentItem, expectedBaseType, false);
            if (EditorGUI.EndChangeCheck())
            {
                var newObj = picked as GameConfig;
                if (newObj != null && refAttribute.TypeConstraint != null)
                {
                    var newType = newObj.GetType();
                    var constraint = refAttribute.TypeConstraint;
                    var valid = constraint == newType ||
                                (refAttribute.AllowInherited && constraint.IsAssignableFrom(newType));
                    if (!valid) newObj = currentItem;
                }

                property.stringValue = newObj ? newObj.Id : string.Empty;
                id = property.stringValue;
                currentItem = newObj;
            }

            if (currentItem != null && GUI.Button(selectRect, "Select"))
            {
                Selection.activeObject = currentItem;
                EditorGUIUtility.PingObject(currentItem);
            }

            row.y += RowHeight + Spacing;

            if (!refAttribute.PreviewOnly)
            {
                EditorGUI.BeginChangeCheck();
                var newId = EditorGUI.TextField(EditorGUI.IndentedRect(row), "Id", id ?? string.Empty);
                if (EditorGUI.EndChangeCheck() && newId != id)
                {
                    property.stringValue = newId;
                }

                row.y += RowHeight + Spacing;
            }

            if (!string.IsNullOrEmpty(property.stringValue) && currentItem == null)
            {
                EditorGUI.HelpBox(row, $"Config with Id '{property.stringValue}' not found.", MessageType.Error);
            }
        }

        private static GameConfig ResolveCurrentItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            try
            {
                return ConfigRootRefsEditor.GetConfig<GameConfig>(id);
            }
            catch (Exception e)
            {
                CustomLog.LogError($"Error retrieving GameConfig with Id '{id}': {e.Message}");
                return null;
            }
        }
    }
}
