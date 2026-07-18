using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.Compare
{
    [Serializable]
    public class GameValueCompare
    {
        [Tooltip("The type of value to compare.")]
        public GameValueType type;
        
        [Tooltip("The mode of comparison.")]
        public CompareMode compareMode;
        
        [Tooltip("The value to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("value")]
        [ShowIf(nameof(type), GameValueType.Integer)]
#endif
        public long valueInteger;

        [Tooltip("The value to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("value")]
        [ShowIf(nameof(type), GameValueType.FloatingPoint)]
#endif
        public decimal valueFloatingPoint;

        [Tooltip("The value to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("value")]
        [ShowIf(nameof(type), GameValueType.String)]
#endif
        public string valueString;

        [Tooltip("The value to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("value")]
        [ShowIf(nameof(type), GameValueType.Bool)]
#endif
        public bool valueBool;

        [Tooltip("The custom comparer to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("comparer")]
        [ShowIf(nameof(type), GameValueType.Structured)]
#endif
        [ConfigRef(typeof(ConfigCustomComparer))]
        public string comparerId;

        [Tooltip("The value to compare.")]
#if ODIN_INSPECTOR
        [InspectorName("value")]
        [ShowIf(nameof(type), GameValueType.Structured)]
#endif
        public string valueJson;
    }
}