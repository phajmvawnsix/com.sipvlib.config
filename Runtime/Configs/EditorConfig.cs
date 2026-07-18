using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class EditorConfig : GameConfig
    {
        public override bool IgnoreInBuild => true;

#if UNITY_EDITOR && ODIN_INSPECTOR
        protected override Texture GetDefaultEditorIcon()
        {
            return Sirenix.Utilities.Editor.EditorIcons.OdinInspectorLogo;
        }
#endif
    }
}