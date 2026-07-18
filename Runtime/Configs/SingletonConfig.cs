using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class SingletonConfig<T> : GameConfig where T : GameConfig
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Application.isPlaying)
                    {
                        // TODO: load from cache first, if not exist, then load from file
                        _instance = CreateInstance<T>();
                    }
                    else
                    {
                        // In editor mode, Find with AssetDatabase
#if UNITY_EDITOR
                        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                        if (guids.Length > 0)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                        }
#endif
                    }
                }
                return _instance;
            }
        }
    }
}