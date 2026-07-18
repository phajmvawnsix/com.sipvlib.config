using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class PrefabConfig : GameConfig
    {
        [SerializeField] protected GameObject _prefab;

        public GameObject Prefab => _prefab;

#if UNITY_EDITOR && ODIN_INSPECTOR
        protected override Texture GetDefaultEditorIcon()
        {
            return Sirenix.Utilities.Editor.EditorIcons.UnityGameObjectIcon;
        }
#endif
    }
}