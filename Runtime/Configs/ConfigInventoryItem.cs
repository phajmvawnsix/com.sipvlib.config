using Alchemy.Inspector;
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class ConfigInventoryItem : GameConfig
    {
        #region Display Settings
        [BoxGroup("Display")]
        [SerializeField]
        [Tooltip("Visual icon for the item")]
        private Sprite _icon;

        [BoxGroup("Display")]
        [SerializeField]
        [Tooltip("Display name of the item")]
        private string _name;

        [BoxGroup("Display")]
        [SerializeField]
        [Tooltip("Detailed description of the item")]
        [TextArea(3, 5)]
        private string _description;

        [BoxGroup("Display")]
        [SerializeField]
        [Tooltip("Type of inventory item")]
        private InventoryItemType _type;

        [BoxGroup("Display")]
        [SerializeField]
        [Tooltip("Order for sorting items in the inventory display")]
        [Range(0, 1000)]
        private int _sortingOrder;
        #endregion

        #region Value Configuration
        [BoxGroup("Value Configuration")]
        [ShowIf(nameof(IsNumericType))]
        [SerializeField]
        [Tooltip("Maximum integer value the item can hold")]
        private long _maxValue;

        [BoxGroup("Value Configuration")]
        [ShowIf(nameof(IsNumericType))]
        [SerializeField]
        [Tooltip("Initial integer value for the item")]
        private long _startValue;
        #endregion

        #region Regeneration Settings
        [BoxGroup("Regeneration")]
        [ShowIf(nameof(IsRegenerating))]
        [SerializeField]
        [Tooltip("Cooldown duration in milliseconds between regeneration ticks")]
        [Min(0)]
        private long _regenerateCooldown;

        [BoxGroup("Regeneration")]
        [ShowIf(nameof(IsRegenerating))]
        [SerializeField]
        [Tooltip("Quantity value to regenerate per tick")]
        [Min(0.1f)]
        private long _regenerateQuantity = 1;
        #endregion

        #region Time Range
        [BoxGroup("Time Range")]
        [ShowIf(nameof(IsTemporary))]
        [SerializeField]
        [Tooltip("When the item becomes active (Unix timestamp in seconds). Use -1 to disable")]
        [Min(-1)]
        private long _startTime = -1;

        [BoxGroup("Time Range")]
        [ShowIf(nameof(IsTemporary))]
        [SerializeField]
        [Tooltip("When the item expires (Unix timestamp in seconds). Use -1 to disable")]
        [Min(-1)]
        private long _endTime = -1;
        #endregion

        #region Properties
        // Serialize Conditions
        private bool IsNumericType => _type.HasFlag(InventoryItemType.Consumable);
        private bool IsRegenerating => _type.HasFlag(InventoryItemType.Consumable) && _type.HasFlag(InventoryItemType.Regenerate);
        private bool IsTemporary => _type.HasFlag(InventoryItemType.Temporary);

        // Display
        public Sprite Icon => _icon;
        public string Name => _name;
        public string Description => _description;
        public InventoryItemType Type => _type;
        public int SortingOrder => _sortingOrder;

        // Value Configuration
        public long MaxValue => _maxValue;
        public long StartValue => _startValue;

        // Regeneration
        public long RegenerateCooldown => _regenerateCooldown;
        public long RegenerateQuantity => _regenerateQuantity;

        // Time Range
        public long StartTime => _startTime;
        public long EndTime => _endTime;
        #endregion
    }
}
