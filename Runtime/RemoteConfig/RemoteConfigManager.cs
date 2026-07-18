using System;
using Cysharp.Threading.Tasks;
using SiPVLib.Debugging;
using UnityEngine;
using SiPVLib.Utilities;

namespace SiPVLib.Config.RemoteConfig
{
    [Serializable]
    public enum RemoteConfigProvider
    {
        FirebaseRemoteConfig,
        UnityRemoteConfig,
    }

    /// <summary>
    /// Manages remote configuration providers and provides a unified interface for accessing config values.
    /// Acts as a facade for different remote config implementations (e.g., Unity, Firebase).
    /// </summary>
    public class RemoteConfigManager : MonoSingleton<RemoteConfigManager>
    {
        [Tooltip("Select the remote configuration provider to use.")]
        [SerializeField] private RemoteConfigProvider currentRemoteConfigProvider = RemoteConfigProvider.FirebaseRemoteConfig;

        private IRemoteConfigProvider _provider;

        /// <summary>
        /// Initializes the selected remote configuration provider.
        /// </summary>
        /// <returns>A UniTask representing the initialization process.</returns>
        public async UniTask<bool> Init()
        {
            switch (currentRemoteConfigProvider)
            {
                case RemoteConfigProvider.FirebaseRemoteConfig:
                    _provider = new FirebaseRemoteConfigProvider();
                    break;
                case RemoteConfigProvider.UnityRemoteConfig:
                    _provider = new UnityRemoteConfigProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await _provider.Init();
            
            await FetchConfigs(); // Fetch initial configs after initialization
            CustomLog.Log($"[RemoteConfigManager] Initialized with {currentRemoteConfigProvider}");
            return true;
        }

        /// <summary>
        /// Fetches the latest configuration values from the active provider.
        /// </summary>
        /// <returns>A UniTask representing the fetch operation.</returns>
        public async UniTask<bool> FetchConfigs()
        {
            if (_provider == null)
            {
                CustomLog.LogError("[RemoteConfigManager] Provider is not initialized!");
                return false;
            }

            await _provider.FetchConfigs();
            CustomLog.Log("[RemoteConfigManager] Configs fetched successfully.");
            return true;
        }
        
        /// <summary>
        /// Retrieves a configuration value as a JSON string.
        /// </summary>
        /// <param name="key">The key of the configuration setting.</param>
        /// <returns>The JSON string value, or an empty string if not found or uninitialized.</returns>
        public string GetJson(string key)
        {
            if (_provider == null)
            {
                CustomLog.LogWarning("[RemoteConfigManager] Provider is not initialized, returning empty string.");
                return string.Empty;
            }

            return _provider.GetJson(key);
        }

        /// <summary>
        /// Retrieves a strongly typed configuration value.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key of the configuration setting.</param>
        /// <returns>The converted value, or the default value for the type if not found or uninitialized.</returns>
        public T GetValue<T>(string key)
        {
            if (_provider == null)
            {
                CustomLog.LogWarning("[RemoteConfigManager] Provider is not initialized, returning default.");
                return default;
            }

            return _provider.GetValue<T>(key);
        }
    }
}