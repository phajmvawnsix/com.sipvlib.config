using Cysharp.Threading.Tasks;
using SiPVLib.Config;
using SiPVLib.Config.Configs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SiPVLib.Config.Configs
{
    /// <summary>
    /// Implemented by <see cref="PipaPlanet.PipaPlanet.Scripts.Utilities.AssetConfig{T}"/> so
    /// <see cref="ConfigRoot.Init"/> can preload assets flagged for startup loading without
    /// depending on the generic asset type.
    /// </summary>
    public interface IAssetPreloadable
    {
        bool LoadAssetOnStartup { get; }
        UniTask PreloadAssetAsync();
    }
}

namespace PipaPlanet.PipaPlanet.Scripts.Utilities
{
    public abstract class AssetConfig<T> : GameConfig, IAssetPreloadable where T : Object
    {
        [Tooltip("If enabled, the asset is loaded during this config's ConfigLocation initialization " +
                 "(ConfigManager.InitXXX). Otherwise it's loaded lazily at runtime via GetAsset()/GetAssetAsync().")]
        [SerializeField] private bool _loadAssetOnStartup;

        [ShowIf(nameof(_storeLocation), ConfigLocation.Local)] [SerializeField]
        private T _asset;

        [HideIf(nameof(_storeLocation), ConfigLocation.Addressable)]
        [HideIf(nameof(_storeLocation), ConfigLocation.Local)]
        [SerializeField]
        private string _assetId;

        [ShowIf(nameof(_storeLocation), ConfigLocation.Addressable)] [SerializeField]
        private AssetReference _addressableReference;
        
        private UniTask<T> _loadTask;
        
        public bool LoadAssetOnStartup => _loadAssetOnStartup;

        public T Asset
        {
            get
            {
                if (_asset || _storeLocation == ConfigLocation.Local)
                {
                    return _asset;
                }



                return _asset;
            }
        }

        /// <summary>Creates a runtime-only config instance with its asset already set. Local store location.</summary>
        protected static TConfig CreateWithAsset<TConfig>(T asset) where TConfig : AssetConfig<T>
        {
            var config = CreateInstance<TConfig>();
            config._asset = asset;
            config._storeLocation = ConfigLocation.Local;
            return config;
        }

        private async UniTask<T> LoadAssetAsync()
        {
            if (_storeLocation == ConfigLocation.Local)
            {
                return _asset;
            }

            if (_storeLocation == ConfigLocation.Addressable)
            {
                var handle = _addressableReference.LoadAssetAsync<T>();
                await handle.Task;
                _asset = handle.Result;
                return _asset;
            }

            if (_storeLocation == ConfigLocation.Resources)
            {
                var resourcePath = _assetId; // Assuming _assetId is the path in Resources
                var resource = await Resources.LoadAsync<T>(resourcePath);
                _asset = (T)resource;
                return _asset;
            }

            // Handle other storage locations if needed
            return null;
        }

        public async UniTask PreloadAssetAsync()
        {
            await GetAssetAsync();
        }

        public async UniTask<T> GetAssetAsync()
        {
            if (_asset)
            {
                return _asset;
            }

            if (_loadTask.Status == UniTaskStatus.Pending)
            {
                return await _loadTask;
            }

            _loadTask = LoadAssetAsync();
            return await _loadTask;
        }
        
        public T GetAsset()
        {
            if (_asset)
            {
                return _asset;
            }

            if (_storeLocation == ConfigLocation.Local)
            {
                return _asset;
            }

            if (_storeLocation == ConfigLocation.Resources)
            {
                var resourcePath = _assetId; // Assuming _assetId is the path in Resources
                _asset = Resources.Load<T>(resourcePath);
                return _asset;
            }
            
            if (_storeLocation == ConfigLocation.Addressable)
            {
                // Force load the asset synchronously (not recommended for large assets)
                var handle = _addressableReference.LoadAssetAsync<T>();
                handle.WaitForCompletion();
                _asset = handle.Result;
                return _asset;
            }

            Debug.LogWarning($"Asset of type {typeof(T)} is not loaded yet. Use GetAssetAsync() to load it asynchronously.");
            return null;
        }
    }
}