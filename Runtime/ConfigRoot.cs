using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SiPVLib.Config.Configs;
using SiPVLib.Debugging;
using SiPVLib.Utilities.Extensions;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config
{
    /// <summary>
    /// Holds the curated set of <see cref="GameConfig"/> items for one <see cref="ConfigLocation"/>
    /// and resolves Id/type lookups against them. Base type for the provider-specific roots
    /// (<see cref="ConfigRootAddressable"/>, <see cref="ConfigRootRemoteConfig"/>) which override
    /// <see cref="Init"/> to bootstrap their source before delegating back to this class.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigRoot", menuName = "SiPV/Config/ConfigRoot", order = 0)]
    public class ConfigRoot : ScriptableObject
    {
        [SerializeField] private ConfigLocation _location;

        [SerializeField] private List<string> _configsId = new();

#if ODIN_INSPECTOR
        [ShowIf(nameof(CanSerializeRefs))]
        [SerializeField, ReadOnly]
        private List<GameConfig> _configsRef = new();
#else
        [SerializeField]
        private List<GameConfig> _configsRef = new();
#endif

        private readonly Dictionary<string, GameConfig> _configsLoaded = new();
        private readonly Dictionary<Type, GameConfig[]> _configsLoadedByType = new();

        // ── Properties ───────────────────────────────────────────────────

        public ConfigLocation Location => _location;

        public bool IsInitialized { get; private set; }

        private bool CanSerializeRefs => _location is ConfigLocation.Addressable or ConfigLocation.Local or ConfigLocation.RemoteConfig;

        public List<GameConfig> GetConfigRefs()
        {
            return _configsRef ?? new List<GameConfig>();
        }

        /// <summary>
        /// Rebuilds the Id- and type-indexed lookup caches from <see cref="_configsId"/>/<see cref="_configsRef"/>.
        /// Subclasses override to bootstrap their source (e.g. Addressables, RemoteConfigManager)
        /// before calling <c>base.Init()</c> to perform this resolution.
        /// </summary>
        public virtual async UniTask<bool> Init()
        {
            _configsLoaded.Clear();
            _configsLoadedByType.Clear();

            var tempDict = new Dictionary<Type, List<GameConfig>>();
            foreach (var itemId in _configsId ?? new List<string>())
            {
                var configItem = (_configsRef ?? new List<GameConfig>()).Find(ci => ci.Id == itemId);
                if (configItem != null)
                {
                    _configsLoaded[itemId] = configItem;

                    var type = configItem.GetType();
                    if (!tempDict.TryGetValue(type, out var typeList))
                    {
                        typeList = new List<GameConfig>();
                        tempDict[type] = typeList;
                    }
                    typeList.Add(configItem);
                }
                else
                {
                    CustomLog.LogWarning($"Config item with ID {itemId} not found in {name}.");
                }
            }

            foreach (var kvp in tempDict)
            {
                _configsLoadedByType[kvp.Key] = kvp.Value.ToArray();
            }

            IsInitialized = true;
            
            return true;
        }

        /// <summary>Looks up a config by Id, cache-first, falling back to a linear scan of <see cref="_configsRef"/>.</summary>
        public GameConfig GetConfig(string itemId)
        {
            if (_configsLoaded.TryGetValue(itemId, out var item))
            {
                return item;
            }

            var configItem = (_configsRef ?? new List<GameConfig>()).Find(ci => ci.Id == itemId);
            
            if (configItem == null) return null;
            
            _configsLoaded[itemId] = configItem;
            return configItem;
        }

        /// <summary>Returns all loaded configs assignable to <typeparamref name="T"/>, optionally including subclasses.</summary>
        public T[] GetConfigs<T>(bool includeChildTypes = true) where T : GameConfig
        {
            if (!includeChildTypes)
            {
                if (!_configsLoadedByType.TryGetValue(typeof(T), out var configItems)) return Array.Empty<T>();

                var result = new T[configItems.Length];
                configItems.CopyTo(result, 0);

                return result;
            }

            var typesKeys = new List<Type>(_configsLoadedByType.Keys);
            var results = new List<GameConfig>();
            foreach (var typeKey in typesKeys)
            {
                if (typeKey.IsSubclassOf(typeof(T)) || typeKey == typeof(T))
                {
                    results.AddRange(_configsLoadedByType[typeKey]);
                }
            }

            return results.ConvertAll(item => (T) item).ToArray();
        }

#if UNITY_EDITOR

        /// <summary>
        /// Editor-only: rewrites this root's Id list (and, for Local/Addressable, its direct
        /// asset refs) from the given set of configs. Skips null items and items with
        /// <see cref="GameConfig.IgnoreInBuild"/> set. Called by MasterWindow's "Update Config Root".
        /// </summary>
        public void UpdateConfigs(ConfigLocation configLocation, GameConfig[] items)
        {
            _location = configLocation;
            _configsId.Clear();
            _configsRef.Clear();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (item.IgnoreInBuild) continue;

                _configsId.Add(item.Id);
                if (configLocation is ConfigLocation.Addressable or ConfigLocation.Local)
                {
                    _configsRef.Add(item);
                }
            }
        }

        public List<string> GetAllIds()
        {
            return _configsId.DeepClone();
        }

#endif
    }
}