using Alchemy.Inspector;
using SiPVLib.Config.Compare;
using SiPVLib.Config.Configs;
using UnityEngine;

namespace SiPVLib.Config.GameConditions
{
    public partial class GameCondition
    {
        [ShowIf(nameof(UserDataSerialized))]
        [SerializeField] private bool _isInventoryItem;

        [ShowIf(nameof(ShowInventoryItemId))]
        [ConfigRef(typeof(ConfigInventoryItem))]
        [SerializeField] private string _inventoryItemId;

        [ShowIf(nameof(UserDataSerialized))]
        [HideIf(nameof(_isInventoryItem))]
        [SerializeField] private string _userDataKey;

        // UserDataSerialized already implies _type == GameConditionType.UserData, so it alone is
        // equivalent to the original's "UserDataSerialized AND _type == UserData".
        [ShowIf(nameof(UserDataSerialized))]
        [SerializeField] private GameValueCompare _valueCompare;

        private bool UserDataSerialized => _type == GameConditionType.UserData && !string.IsNullOrEmpty(UserDataKey);
        private bool ShowInventoryItemId => UserDataSerialized && _isInventoryItem;
        public string UserDataKey => _isInventoryItem ? _inventoryItemId : _userDataKey;
        public GameValueCompare ValueCompare => _valueCompare;
    }
}
