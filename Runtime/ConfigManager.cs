using System;
using System.Collections.Generic;
using System.Linq;
using SiPVLib.Config.Configs;
using SiPVLib.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using SiPVLib.Config.RemoteConfig;
using SiPVLib.Debugging;
using SiPVLib.Event;

namespace SiPVLib.Config
{
    /// <summary>
    /// Manages the configuration data for the game, handling local, remote, resources, and addressable configurations.
    /// Acts as a central point for accessing game configuration.
    /// </summary>
    public class ConfigManager : MonoSingleton<ConfigManager>
    {
        [Tooltip("Configuration root for local configs.")]
        [SerializeField] private ConfigRoot _configLocal;
        [Tooltip("Configuration root for remote configs.")]
        [SerializeField] private ConfigRoot _configRemote;
        [Tooltip("Configuration root for addressable configs.")]
        [SerializeField] private AssetReference _configAddressableRef;
        [Tooltip("Configuration root for resources configs.")]
#if ODIN_INSPECTOR
        [SerializeField, Sirenix.OdinInspector.FilePath(Extensions = ".asset", RequireExistingPath = true)] private string _configResourcesPath;
#else
        [SerializeField] private string _configResourcesPath;
#endif

        /// <summary>
        /// Configuration root loaded from Resources.
        /// </summary>
        private ConfigRoot _configResources;

        /// <summary>
        /// Configuration root loaded from Addressables.
        /// </summary>
        private ConfigRoot _configAddressable;

        /// <summary>
        /// Checks if the local configuration is initialized.
        /// </summary>
        private bool IsInitializedLocal => _configLocal != null && _configLocal.IsInitialized;

        /// <summary>
        /// Checks if the resources configuration is initialized.
        /// </summary>
        private bool IsInitializedResources => string.IsNullOrWhiteSpace(_configResourcesPath) || _configResources.IsInitialized;

        /// <summary>
        /// Checks if the addressable configuration is initialized.
        /// </summary>
        private bool IsInitializedAddressable => _configAddressableRef == null || _configAddressable.IsInitialized;

        /// <summary>
        /// Checks if the remote configuration is initialized.
        /// </summary>
        private bool IsInitializedRemoteConfig => _configRemote == null || _configRemote.IsInitialized;
        
        public bool IsFullInitialized => IsInitializedLocal && IsInitializedResources && IsInitializedAddressable && IsInitializedRemoteConfig;

        private bool _fullyInitializedBroadcast;

        /// <summary>
        /// Unity Awake method. Initializes local configurations.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            InitLocal();
        }

        /// <summary>
        /// Initializes the local configuration root.
        /// </summary>
        private void InitLocal()
        {
            InitLocalAsync().Forget();
        }

        private async UniTaskVoid InitLocalAsync()
        {
            var success = await _configLocal.Init();
            BroadcastLocationInitialized(ConfigLocation.Local, success);
        }

        /// <summary>
        /// Initializes the remote configuration root and updates it with values from RemoteConfigManager.
        /// </summary>
        public static async UniTask<bool> InitRemoteConfig()
        {
            var manager = Instance;
            if (manager == null || manager._configRemote == null)
            {
                CustomLog.LogWarning("ConfigManager or configRemote is not assigned.");
                BroadcastLocationInitialized(ConfigLocation.RemoteConfig, false);
                return false;
            }
            await manager._configRemote.Init();

            var configRef = manager._configRemote.GetConfigRefs();
            foreach (var config in configRef)
            {
                if (config == null) continue;

                var key = config.RemoteConfigKey;
                // Fetch value from remote config manager
                var value = RemoteConfigManager.Instance.GetJson(key);
                config.SetRemoteConfig(value);
            }

            BroadcastLocationInitialized(ConfigLocation.RemoteConfig, true);
            return true;
        }

        /// <summary>
        /// Asynchronously initializes the configuration root from Resources.
        /// </summary>
        /// <returns>True if initialization was successful, otherwise false.</returns>
        public static async UniTask<bool> InitResources()
        {
            var manager = Instance;

            if (manager._configResources != null)
            {
                return true; // Already initialized
            }

            var resourcesPath = manager._configResourcesPath;

            // Get resources relative path
            var resourcesIndex = resourcesPath.LastIndexOf("Resources/", StringComparison.Ordinal);
            if (resourcesIndex >= 0)
            {
                resourcesPath = resourcesPath.Skip(resourcesIndex + "Resources/".Length).ToArray().ToString();
            }

            // Load ConfigRoot from Resources
            var handle = await Resources.LoadAsync<ConfigRoot>(resourcesPath);
            if (handle == null)
            {
                CustomLog.LogError("Failed to load Resources ConfigRoot.");
                BroadcastLocationInitialized(ConfigLocation.Resources, false);
                return false;
            }

            if (handle is ConfigRoot gameMasterData)
            {
                manager._configResources = gameMasterData;
                if (manager._configResources != null)
                {
                    await manager._configResources.Init();
                    BroadcastLocationInitialized(ConfigLocation.Resources, true);
                    return true;
                }
            }

            CustomLog.LogError("Failed to load Resources ConfigRoot.");
            BroadcastLocationInitialized(ConfigLocation.Resources, false);
            return false;
        }

        /// <summary>
        /// Asynchronously initializes the configuration root from Addressables.
        /// </summary>
        /// <returns>True if initialization was successful, otherwise false.</returns>
        public static async UniTask<bool> InitAddressable()
        {
            var manager = Instance;
            if (manager._configAddressable != null)
            {
                return true; // Already initialized
            }

            // Load ConfigRoot from Addressables
            var handle = manager._configAddressableRef.LoadAssetAsync<ConfigRoot>();
            await handle.Task;
            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                CustomLog.LogError("Failed to load Addressable ConfigRoot.");
                BroadcastLocationInitialized(ConfigLocation.Addressable, false);
                return false;
            }

            manager._configAddressable = handle.Result;
            if (manager._configAddressable != null)
            {
                await manager._configAddressable.Init();
                BroadcastLocationInitialized(ConfigLocation.Addressable, true);
                return true;
            }

            CustomLog.LogError("Failed to load Addressable ConfigRoot.");
            BroadcastLocationInitialized(ConfigLocation.Addressable, false);
            return false;
        }

        /// <summary>
        /// Broadcasts <see cref="ConfigLocationInitializedEvent"/> for one location, then checks
        /// whether every location has now completed its init attempt and, the first time that
        /// becomes true, broadcasts <see cref="ConfigFullyInitializedEvent"/>.
        /// </summary>
        private static void BroadcastLocationInitialized(ConfigLocation location, bool success)
        {
            EventManager.Invoke(new ConfigLocationInitializedEvent { Location = location, Success = success });

            var manager = Instance;
            if (manager._fullyInitializedBroadcast || !manager.IsFullInitialized) return;

            manager._fullyInitializedBroadcast = true;
            EventManager.Invoke(new ConfigFullyInitializedEvent());
        }

        /// <summary>
        /// Retrieves a specific game configuration by its item ID.
        /// </summary>
        /// <typeparam name="T">The type of the game configuration.</typeparam>
        /// <param name="itemId">The ID of the configuration item.</param>
        /// <param name="location">The location to search for the configuration (default is Local).</param>
        /// <param name="findAllIfNotFound">If true, searches all locations if not found in the specified location.</param>
        /// <returns>The game configuration if found, otherwise null.</returns>
        public static T Get<T>(
            string itemId,
            ConfigLocation location = ConfigLocation.Local,
            bool findAllIfNotFound = false) where T : GameConfig
        {
            if (TryGet<T>(itemId, out var result, location, findAllIfNotFound)) return result;

            if (!findAllIfNotFound)
            {
                CustomLog.LogWarning($"GameConfig with ID '{itemId}' not found in location '{location}'.");
            }

            return null;
        }

        /// <summary>
        /// Same lookup as <see cref="Get{T}"/> but returns false instead of logging a warning when
        /// not found — for call sites where "not found" is an expected, handled case.
        /// </summary>
        public static bool TryGet<T>(
            string itemId,
            out T config,
            ConfigLocation location = ConfigLocation.Local,
            bool findAllIfNotFound = false) where T : GameConfig
        {
            var manager = Instance;

            var result = location switch
            {
                ConfigLocation.Resources    => manager._configResources.GetConfig(itemId),
                ConfigLocation.Addressable  => manager._configAddressable.GetConfig(itemId),
                ConfigLocation.Local        => manager._configLocal.GetConfig(itemId),
                ConfigLocation.RemoteConfig => manager._configRemote.GetConfig(itemId),
                _                            => null
            };

            if (result == null && findAllIfNotFound)
            {
                result = manager._configLocal.GetConfig(itemId)
                         ?? manager._configResources.GetConfig(itemId)
                         ?? manager._configAddressable.GetConfig(itemId)
                         ?? manager._configRemote.GetConfig(itemId);
            }

            config = result as T;
            return config != null;
        }

        /// <summary>
        /// Retrieves all game configurations of a specific type from all locations.
        /// </summary>
        /// <typeparam name="T">The type of the game configuration.</typeparam>
        /// <returns>An array of game configurations of the specified type.</returns>
        public static T[] GetAll<T>() where T : GameConfig
        {
            var results = new List<T>();
            results.AddRange(GetAll<T>(ConfigLocation.Local));
            results.AddRange(GetAll<T>(ConfigLocation.Resources));
            results.AddRange(GetAll<T>(ConfigLocation.Addressable));
            results.AddRange(GetAll<T>(ConfigLocation.RemoteConfig));
            return results.ToArray();
        }
        
        /// <summary>
        /// Retrieves all game configurations of a specific type from a specific location.
        /// </summary>
        /// <typeparam name="T">The type of the game configuration.</typeparam>
        /// <param name="location">The location to search for the configurations.</param>
        /// <returns>An array of game configurations of the specified type.</returns>
        public static T[] GetAll<T>(ConfigLocation location) where T : GameConfig
        {
            var manager = Instance;
            var results = Array.Empty<T>();
            switch (location)
            {
                case ConfigLocation.Resources:
                    if (manager._configResources != null && manager._configResources.IsInitialized)
                    {
                        results = manager._configResources.GetConfigs<T>();
                    }
                    break;
                case ConfigLocation.Addressable:
                    if (manager._configAddressable != null && manager._configAddressable.IsInitialized)
                    {
                        results = manager._configAddressable.GetConfigs<T>();
                    }
                    break;
                case ConfigLocation.Local:
                    results = manager._configLocal.GetConfigs<T>();
                    break;
                case ConfigLocation.RemoteConfig:
                    if (manager._configRemote != null && manager._configRemote.IsInitialized)
                    {
                        results = manager._configRemote.GetConfigs<T>();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }

            return results;
        }
    }
}