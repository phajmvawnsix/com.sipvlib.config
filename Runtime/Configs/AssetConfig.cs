using Cysharp.Threading.Tasks;
using SiPVLib.Config;
using SiPVLib.Config.Configs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PipaPlanet.PipaPlanet.Scripts.Utilities
{
    public abstract class AssetConfig<T> : GameConfig where T : Object
    {
        [ShowIf(nameof(_storeLocation), ConfigLocation.Local)] [SerializeField]
        private T _asset;

        [HideIf(nameof(_storeLocation), ConfigLocation.Addressable)]
        [HideIf(nameof(_storeLocation), ConfigLocation.Local)]
        [SerializeField]
        private string _assetId;

        [ShowIf(nameof(_storeLocation), ConfigLocation.Addressable)] [SerializeField]
        private AssetReference _addressableReference;
        
        private UniTask<T> _loadTask;

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
#if UNITY_EDITOR
            set { _asset = value; }
#endif
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
                return handle.Result;
            }

            if (_storeLocation == ConfigLocation.Resources)
            {
                var resourcePath = _assetId; // Assuming _assetId is the path in Resources
                var resource = await Resources.LoadAsync<T>(resourcePath);
                return (T)resource;
            }

            // Handle other storage locations if needed
            return null;
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