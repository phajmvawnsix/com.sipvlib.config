using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class EditorConfig : GameConfig
    {
        public override bool IgnoreInBuild => true;

#if UNITY_EDITOR
        protected override Texture GetDefaultEditorIcon()
        {
            return UnityEditor.EditorGUIUtility.IconContent("SettingsIcon").image as Texture;
        }
#endif
    }
}
