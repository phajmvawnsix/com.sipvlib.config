using System;
using Alchemy.Inspector;
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
        [InspectorName("value")]
        [ShowIf(nameof(IsInteger))]
        public long valueInteger;

        [Tooltip("The value to compare.")]
        [InspectorName("value")]
        [ShowIf(nameof(IsFloatingPoint))]
        public decimal valueFloatingPoint;

        [Tooltip("The value to compare.")]
        [InspectorName("value")]
        [ShowIf(nameof(IsString))]
        public string valueString;

        [Tooltip("The value to compare.")]
        [InspectorName("value")]
        [ShowIf(nameof(IsBool))]
        public bool valueBool;

        [Tooltip("The custom comparer to compare.")]
        [InspectorName("comparer")]
        [ShowIf(nameof(IsStructured))]
        [ConfigRef(typeof(ConfigCustomComparer))]
        public string comparerId;

        [Tooltip("The value to compare.")]
        [InspectorName("value")]
        [ShowIf(nameof(IsStructured))]
        public string valueJson;

        private bool IsInteger => type == GameValueType.Integer;
        private bool IsFloatingPoint => type == GameValueType.FloatingPoint;
        private bool IsString => type == GameValueType.String;
        private bool IsBool => type == GameValueType.Bool;
        private bool IsStructured => type == GameValueType.Structured;
    }
}
