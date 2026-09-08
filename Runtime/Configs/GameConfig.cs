using Alchemy.Inspector;
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    /// <summary>
    /// Base ScriptableObject for all game configuration items. Carries an Id used for
    /// <see cref="ConfigRefAttribute"/> lookups (resolved via the Editor-only Id cache) and a
    /// declared <see cref="ConfigLocation"/> describing which storage source is expected to serve it.
    /// </summary>
    public class GameConfig : ScriptableObject
    {
        // Basic info
        [HorizontalGroup("Split")]
        [BoxGroup("Split/Basic Info")]
#if UNITY_EDITOR
        [ValidateInput(nameof(ValidateIdUnique), "Duplicate Id used by another config.")]
        [OnValueChanged(nameof(OnConfigEdited))]
#endif
        [SerializeField] protected string _id;

        [BoxGroup("Split/Basic Info")]
        [SerializeField] protected string _configName;

        // Storage settings
        [HorizontalGroup("Split")]
        [BoxGroup("Split/Storage Settings")]
#if UNITY_EDITOR
        [OnValueChanged(nameof(OnConfigEdited))]
#endif
        [SerializeField] protected bool _ignoreInBuild;

#if UNITY_EDITOR
        [ValidateInput(nameof(ValidateLocationField), "Actual config location is not match.")]
        [OnValueChanged(nameof(OnStoreLocationEdited))]
#endif
        [BoxGroup("Split/Storage Settings")]
        [HideIf(nameof(IgnoreInBuild))]
        [SerializeField] protected ConfigLocation _storeLocation;

        // Only meaningful when the config is actually stored remotely — showing it for
        // Local/Resources/Addressable configs was pure noise.
        [BoxGroup("Split/Storage Settings")]
        [ShowIf(nameof(ShowRemoteConfigKey))]
        [SerializeField] protected string _remoteConfigKey;

        // ── Properties ───────────────────────────────────────────────────

        public string Id => _id;
        public virtual bool IgnoreInBuild => _ignoreInBuild;
        public string ConfigName => _configName;
        public ConfigLocation StoreLocation => _storeLocation;
        public string RemoteConfigKey => _remoteConfigKey;

        /// <summary>
        /// Backs the inspector condition on <see cref="_remoteConfigKey"/>: only relevant when the
        /// config is actually built and stored remotely. Checks the virtual <see cref="IgnoreInBuild"/>
        /// property (not the raw field) so a subclass override — e.g. <see cref="EditorConfig"/>,
        /// which is always excluded from build — is respected without needing its own override here.
        /// </summary>
        private bool ShowRemoteConfigKey => !IgnoreInBuild && _storeLocation == ConfigLocation.RemoteConfig;

#if UNITY_EDITOR

        // Tracks the location this asset was in before the current edit, so a StoreLocation change
        // can refresh both the old and new ConfigRoot instead of only the newly-selected one.
        private ConfigLocation _lastKnownLocation;

        private void OnEnable()
        {
            _lastKnownLocation = _storeLocation;
        }

        /// <summary>Backs the Alchemy ValidateInput warning on <see cref="_id"/> for live duplicate detection.</summary>
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

        /// <summary>Backs the Alchemy ValidateInput warning on <see cref="_storeLocation"/>.</summary>
        private bool ValidateLocationField(ConfigLocation location) => ValidateLocation(location);

        /// <summary>
        /// Checks that the asset's actual project location (folder / Addressable / Resources
        /// entry) matches the declared <see cref="_storeLocation"/>. Backs the Alchemy ValidateInput
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

        /// <summary>Icon shown in MasterWindow's menu tree; flags configs excluded from build.</summary>
        public Texture GetEditorIcon()
        {
            return IgnoreInBuild
                ? UnityEditor.EditorGUIUtility.IconContent("d_winbtn_mac_close").image as Texture
                : GetDefaultEditorIcon();
        }

        /// <summary>Override per subtype to show a more specific icon than the default Unity logo.</summary>
        protected virtual Texture GetDefaultEditorIcon()
        {
            return UnityEditor.EditorGUIUtility.IconContent("UnityLogo").image as Texture;
        }
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
