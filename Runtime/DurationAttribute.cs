using System;

namespace SiPVLib.Config
{
    /// <summary>
    /// Marks a long field for duration/offset selection in the editor.
    /// The value is stored as seconds (relative duration from a reference point, typically "now").
    ///
    /// The accompanying editor drawer provides a duration picker UI with convenient presets
    /// (e.g., "1 hour", "1 day", "1 week") and manual input.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DurationAttribute : Attribute
    {
        /// <summary>
        /// Whether to allow -1 (disabled/unlimited). Default: true.
        /// </summary>
        public bool AllowDisabled { get; }

        /// <summary>
        /// Optional label override for the field. Default: null (uses field name).
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Unit of duration display. Default: Seconds.
        /// </summary>
        public DurationUnit Unit { get; }

        public DurationAttribute(bool allowDisabled = true, string label = null, DurationUnit unit = DurationUnit.Seconds)
        {
            AllowDisabled = allowDisabled;
            Label = label;
            Unit = unit;
        }

        /// <summary>
        /// Display units for duration values.
        /// </summary>
        public enum DurationUnit
        {
            /// <summary>Display as seconds (default)</summary>
            Seconds = 0,

            /// <summary>Display as minutes (divides by 60)</summary>
            Minutes = 1,

            /// <summary>Display as hours (divides by 3600)</summary>
            Hours = 2,

            /// <summary>Display as days (divides by 86400)</summary>
            Days = 3,
        }
    }
}

