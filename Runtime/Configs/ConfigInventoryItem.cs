#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class ConfigInventoryItem : GameConfig
    {
        #region Display Settings
#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Visual icon for the item")]
        private Sprite _icon;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Display name of the item")]
        private string _name;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Detailed description of the item")]
        [TextArea(3, 5)]
        private string _description;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Type of inventory item")]
        private InventoryItemType _type;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Order for sorting items in the inventory display")]
        [Range(0, 1000)]
        private int _sortingOrder;
        #endregion

        #region Value Configuration
#if ODIN_INSPECTOR
        [BoxGroup("Value Configuration")]
        [ShowIf(nameof(IsNumericType))]
#endif
        [SerializeField]
        [Tooltip("Maximum integer value the item can hold")]
        private long _maxValue;

#if ODIN_INSPECTOR
        [BoxGroup("Value Configuration")]
        [ShowIf(nameof(IsNumericType))]
#endif
        [SerializeField]
        [Tooltip("Initial integer value for the item")]
        private long _startValue;
        #endregion

        #region Regeneration Settings
#if ODIN_INSPECTOR
        [BoxGroup("Regeneration")]
        [ShowIf(nameof(IsRegenerating))]
#endif
        [SerializeField]
        [Tooltip("Cooldown duration in milliseconds between regeneration ticks")]
#if ODIN_INSPECTOR
        [MinValue(0)]
#endif
        private long _regenerateCooldown;

#if ODIN_INSPECTOR
        [BoxGroup("Regeneration")]
        [ShowIf(nameof(IsRegenerating))]
#endif
        [SerializeField]
        [Tooltip("Quantity value to regenerate per tick")]
#if ODIN_INSPECTOR
        [MinValue(0.1)]
#endif
        private long _regenerateQuantity = 1;
        #endregion

        #region Time Range
#if ODIN_INSPECTOR
        [BoxGroup("Time Range")]
        [ShowIf(nameof(IsTemporary))]
#endif
        [SerializeField]
        [Tooltip("When the item becomes active (Unix timestamp in seconds). Use -1 to disable")]
#if ODIN_INSPECTOR
        [MinValue(-1)]
#endif
        private long _startTime = -1;

#if ODIN_INSPECTOR
        [BoxGroup("Time Range")]
        [ShowIf(nameof(IsTemporary))]
#endif
        [SerializeField]
        [Tooltip("When the item expires (Unix timestamp in seconds). Use -1 to disable")]
#if ODIN_INSPECTOR
        [MinValue(-1)]
#endif
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