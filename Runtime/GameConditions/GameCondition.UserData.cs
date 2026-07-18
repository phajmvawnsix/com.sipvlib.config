using SiPVLib.Config.Compare;
using SiPVLib.Config.Configs;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.GameConditions
{
    public partial class GameCondition
    {
#if ODIN_INSPECTOR
        [ShowIf(nameof(UserDataSerialized))]
#endif
        [SerializeField] private bool _isInventoryItem;

#if ODIN_INSPECTOR
        [ShowIf(nameof(UserDataSerialized))]
        [ShowIf(nameof(_isInventoryItem))]
#endif
        [ConfigRef(typeof(ConfigInventoryItem))]
        [SerializeField] private string _inventoryItemId;

#if ODIN_INSPECTOR
        [ShowIf(nameof(UserDataSerialized))]
        [HideIf(nameof(_isInventoryItem))]
#endif
        [SerializeField] private string _userDataKey;

#if ODIN_INSPECTOR
        [ShowIf(nameof(UserDataSerialized))]
        [ShowIf(nameof(_type), GameConditionType.UserData)]
#endif
        [SerializeField] private GameValueCompare _valueCompare;
        
        private bool UserDataSerialized => _type == GameConditionType.UserData && !string.IsNullOrEmpty(UserDataKey);
        public string UserDataKey => _isInventoryItem ? _inventoryItemId : _userDataKey;
        public GameValueCompare ValueCompare => _valueCompare;
    }
}