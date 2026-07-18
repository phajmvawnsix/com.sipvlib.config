using System;

namespace SiPVLib.Config
{
    /// <summary>
    /// Marks a long field for absolute time selection in the editor.
    /// The value is stored as Unix timestamp (seconds since 1970-01-01 00:00:00 UTC).
    ///
    /// The accompanying editor drawer provides a date/time picker UI for convenient selection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TimeAttribute : Attribute
    {
        /// <summary>
        /// Whether to allow -1 (disabled/unlimited). Default: true.
        /// </summary>
        public bool AllowDisabled { get; }

        /// <summary>
        /// Optional label override for the field. Default: null (uses field name).
        /// </summary>
        public string Label { get; }

        public TimeAttribute(bool allowDisabled = true, string label = null)
        {
            AllowDisabled = allowDisabled;
            Label = label;
        }
    }
}

