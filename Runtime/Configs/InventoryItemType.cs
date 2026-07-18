using System;
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    [Serializable, Flags]
    public enum InventoryItemType
    {
        None = 0,
        
        [Tooltip("Items that can be consumed, such as coins, gems, or other resources.\n" +
                 "These items are typically used for in-game purchases, upgrades, or other actions that require spending resources.\n" +
                 "They can be replenished through gameplay or by purchasing more.")]
        Consumable = 1,
        
        [Tooltip("Items that are temporary and may have a limited duration or usage.\n" +
                 "Examples include event items, boosters, or time-limited offers.")]
        Temporary = 1 << 1,
        
        [Tooltip("Items that can be automatically generated or replenished over time, such as daily rewards, energy refills, or other resources that regenerate.\n" +
                 "These items are typically used to encourage regular engagement with the game and can be replenished without player intervention.")]
        Regenerate = 1 << 2,
        
        [Tooltip("Items that are non-consumable and cannot be used up or consumed, such as skins, characters, or other cosmetic items.\n" +
                 "These items are typically owned indefinitely and do not have a quantity that can be depleted through use.\n" +
                 "They may be obtained through gameplay, events, or purchases and are often used to customize the player's experience without affecting gameplay mechanics.")]
        NoneConsumable = 1 << 3,
    }
}