#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    /// <summary>
    /// Base ScriptableObject for all game configuration items. Carries an Id used for
    /// <see cref="ConfigRefAttribute"/> lookups (resolved via the Editor-only Id cache) and a
    /// declared <see cref="ConfigLocation"/> describing which storage source is expected to serve it.
    /// </summary>
#if ODIN_INSPECTOR
    // With Odin installed, private/non-serialized fields are also serialized via Odin's serializer.
    public class GameConfig : SerializedScriptableObject
#else
    public class GameConfig : ScriptableObject
#endif
    {
        // Basic info
#if ODIN_INSPECTOR
        [HorizontalGroup("Split", 0.5f)]
        [BoxGroup("Split/Basic Info")]
#endif
#if UNITY_EDITOR && ODIN_INSPECTOR
        [ValidateInput(nameof(ValidateIdUnique), "Duplicate Id used by another config.")]
        [OnValueChanged(nameof(OnConfigEdited))]
#endif
        [SerializeField] protected string _id;

#if ODIN_INSPECTOR
        [BoxGroup("Split/Basic Info")]
#endif
        [SerializeField] protected string _configName;

        // Storage settings
#if ODIN_INSPECTOR
        [HorizontalGroup("Split")]
        [BoxGroup("Split/Storage Settings")]
#endif
#if UNITY_EDITOR && ODIN_INSPECTOR
        [OnValueChanged(nameof(OnConfigEdited))]
#endif
        [SerializeField] protected bool _ignoreInBuild;
#if UNITY_EDITOR && ODIN_INSPECTOR
        [ValidateInput(nameof(ValidateLocation), "Actual config location is not match.")]
        [OnValueChanged(nameof(OnStoreLocationEdited))]
#endif
#if ODIN_INSPECTOR
        [BoxGroup("Split/Storage Settings")]
        [HideIf(nameof(_ignoreInBuild))]
#endif
        [SerializeField] protected ConfigLocation _storeLocation;

#if ODIN_INSPECTOR
        [BoxGroup("Split/Storage Settings")]
        [HideIf(nameof(_ignoreInBuild))]
#endif
        [SerializeField] protected string _remoteConfigKey;

        // ── Properties ───────────────────────────────────────────────────

        public string Id => _id;
        public virtual bool IgnoreInBuild => _ignoreInBuild;
        public string ConfigName => _configName;
        public ConfigLocation StoreLocation => _storeLocation;
        public string RemoteConfigKey => _remoteConfigKey;

#if UNITY_EDITOR

        // Tracks the location this asset was in before the current edit, so a StoreLocation change
        // can refresh both the old and new ConfigRoot instead of only the newly-selected one.
        private ConfigLocation _lastKnownLocation;

        private void OnEnable()
        {
            _lastKnownLocation = _storeLocation;
        }

        /// <summary>Backs the Odin ValidateInput warning on <see cref="_id"/> for live duplicate detection.</summary>
        private bool ValidateIdUnique(string id) => !ConfigRootEditorSync.HasDuplicateId(id, this);

        /// <summary>Rebuilds this config's ConfigRoot immediately after an Id/IgnoreInBuild edit.</summary>
        private void OnConfigEdited()
        {
            ConfigRootEditorSync.RefreshLocation(_storeLocation);
        }

        /// <summary>Rebuilds both the previous and newly-selected ConfigRoot after a StoreLocation edit.</summary>
        private void OnStoreLocationEdited()
        {
            if (_lastKnownLocation != _storeLocation)
            {
                ConfigRootEditorSync.RefreshLocation(_lastKnownLocation);
            }

            ConfigRootEditorSync.RefreshLocation(_storeLocation);
            _lastKnownLocation = _storeLocation;
        }

        /// <summary>
        /// Checks that the asset's actual project location (folder / Addressable / Resources
        /// entry) matches the declared <see cref="_storeLocation"/>. Backs the Odin ValidateInput
        /// warning on the field so mismatches are caught in the Inspector rather than at runtime.
        /// </summary>
        public bool ValidateLocation(ConfigLocation location)
        {
            switch (location)
            {
                case ConfigLocation.Resources:
                    // Validate if the asset is indeed in Resources folder
                    var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                    if (!path.Contains("Resources")) return false;
                    break;
                case ConfigLocation.Addressable:
                    // Validate if the asset is marked as Addressable
                    var guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
                    var entry = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
                    if (entry == null) return false;
                    break;
                case ConfigLocation.RemoteConfig:
                    break;
                case ConfigLocation.Local:
                    // Validate if the asset is outside StreamingAssets, Resources, and Addressable
                    var localPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                    if (localPath.Contains("Resources") || localPath.Contains("StreamingAssets")) return false;
                    var localGuid = UnityEditor.AssetDatabase.AssetPathToGUID(localPath);
                    var localEntry = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(localGuid);
                    if (localEntry != null) return false;
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool IsValid() => string.IsNullOrWhiteSpace(GetInvalidReason());

        /// <summary>Override to report a validation problem shown in MasterWindow; empty means valid.</summary>
        public virtual string GetInvalidReason() => string.Empty;

#if ODIN_INSPECTOR
        /// <summary>Icon shown in MasterWindow's menu tree; flags configs excluded from build.</summary>
        public Texture GetEditorIcon()
        {
            return _ignoreInBuild ? Sirenix.Utilities.Editor.EditorIcons.X.Active : GetDefaultEditorIcon();
        }

        /// <summary>Override per subtype to show a more specific icon than the default Unity logo.</summary>
        protected virtual Texture GetDefaultEditorIcon()
        {
            return Sirenix.Utilities.Editor.EditorIcons.UnityLogo;
        }
#endif
#endif
        
        /// <summary>
        /// Update this config with remote config value.
        /// Using reflection to set value to avoid direct dependency on remote config system.
        /// </summary>
        /// <param name="value">Json string</param>
        public virtual void SetRemoteConfig(string value)
        {
            JsonUtility.FromJsonOverwrite(value, this);
        }
    }
}