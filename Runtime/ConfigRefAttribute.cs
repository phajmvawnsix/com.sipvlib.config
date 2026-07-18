using System;

namespace SiPVLib.Config
{
    /// <summary>
    /// Runtime-safe attribute to reference a ConfigItem by storing only its Id in a string field.
    /// This attribute has no editor-only dependencies and can live in any assembly.
    /// The accompanying editor drawer (in an Editor folder) provides rich UI, drag & drop,
    /// validation, and type filtering when the Unity Editor is present (Odin Inspector required).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigRefAttribute : Attribute
    {
        /// <summary>
        /// Optional type constraint. When supplied, only ConfigItems whose concrete type matches this
        /// constraint (or inherits from it if <see cref="AllowInherited"/> is true) are accepted by the editor drawer.
        /// </summary>
        public Type TypeConstraint { get; }

        /// <summary>
        /// Whether inherited types of <see cref="TypeConstraint"/> are allowed. Default: true.
        /// </summary>
        public bool AllowInherited { get; }

        /// <summary>
        /// When true, only shows the preview object field, hiding the label and ID text field. Default: false.
        /// </summary>
        public bool PreviewOnly { get; }

        public ConfigRefAttribute()
        {
        }

        public ConfigRefAttribute(Type typeConstraint, bool allowInherited = true, bool previewOnly = false)
        {
            TypeConstraint = typeConstraint;
            AllowInherited = allowInherited;
            PreviewOnly = previewOnly;
        }

        public ConfigRefAttribute(bool previewOnly = false)
        {
            PreviewOnly = previewOnly;
        }
    }
}