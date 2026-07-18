#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using UnityEditor;

namespace SiPVLib.Config
{
    public static class ConfigRootRefsEditor
    {
        private static ConfigRoot _localRoot;
        private static ConfigRoot _resourcesRoot;
        private static ConfigRoot _addressableRoot;
        private static ConfigRoot _remoteConfigRoot;

        private static Dictionary<string, GameConfig> _configsRef = new();
        private static Dictionary<Type, List<GameConfig>> _configsTypeRef = new();

        public static ConfigRoot LocalRoot
        {
            get
            {
                if (_localRoot == null)
                {
                    InitCache();
                }

                return _localRoot;
            }
        }

        public static ConfigRoot ResourcesRoot
        {
            get
            {
                if (_resourcesRoot == null)
                {
                    InitCache();
                }

                return _resourcesRoot;
            }
        }

        public static ConfigRoot AddressableRoot
        {
            get
            {
                if (_addressableRoot == null)
                {
                    InitCache();
                }

                return _addressableRoot;
            }
        }
        
        public static ConfigRoot RemoteConfigRoot
        {
            get
            {
                if (_remoteConfigRoot == null)
                {
                    InitCache();
                }

                return _remoteConfigRoot;
			}
		}

        private static void InitCache()
        {
            var guids = AssetDatabase.FindAssets("t:ConfigRoot", MasterWindowSettings.instance.AllRootFolders());
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var gameMasterData = AssetDatabase.LoadAssetAtPath<ConfigRoot>(path);
                if (gameMasterData != null)
                {
                    switch (gameMasterData.Location)
                    {
                        case ConfigLocation.Local:
                            _localRoot = gameMasterData;
                            break;
                        case ConfigLocation.Resources:
                            _resourcesRoot = gameMasterData;
                            break;
                        case ConfigLocation.Addressable:
                            _addressableRoot = gameMasterData;
                            break;
                        case ConfigLocation.RemoteConfig:
                            _remoteConfigRoot = gameMasterData;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
        
        public static void UpdateCache()
        {
            _configsRef.Clear();
            _configsTypeRef.Clear();

            var guids = AssetDatabase.FindAssets("t:GameConfig", MasterWindowSettings.instance.AllRootFolders());
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                
                if (config == null || string.IsNullOrWhiteSpace(config.Id)) continue;
                
                if (!_configsRef.TryAdd(config.Id, config))
                {
                    CustomLog.LogError("Duplicate Config Id found: " + config.Id);
                }

                var type = config.GetType();
                if (!_configsTypeRef.ContainsKey(type))
                {
                    _configsTypeRef[type] = new List<GameConfig>();
                }
                _configsTypeRef[type].Add(config);
            }
        }

        public static T GetConfig<T>(string key) where T : GameConfig
        {
            T result;
            
            if (_configsRef.TryGetValue(key, out var item) && item != null)
            {
                result = item as T;
                if (result != null)
                {
                    return result;
                }
                
                CustomLog.LogError($"Config with Id '{key}' is not a instance of '{typeof(T).Name}'.");
                return null;
            }

            UpdateCache();

            if (!_configsRef.TryGetValue(key, out item) || item == null) return null;
            result = item as T;
            if (result != null)
            {
                return result;
            }
                
            CustomLog.LogError($"Config with Id '{key}' is not a instance of '{typeof(T).Name}'.");
            return null;
        }

        public static List<T> GetConfigs<T>(bool includedChildTypes = true) where T : GameConfig
        {
            var result = new List<T>();
            var targetType = typeof(T);
            
            foreach (var (type, value) in _configsTypeRef)
            {
                if (type != targetType && (!includedChildTypes || !type.IsSubclassOf(targetType))) continue;
                
                foreach (var item in value)
                {
                    if (item is T typedItem)
                    {
                        result.Add(typedItem);
                    }
                }
            }

            return result;
        }
    }
}
#endif