using System;

namespace SiPVLib.Config
{
    /// <summary>
    /// Groups a <see cref="Configs.GameConfig"/> subtype under a named category in the Master
    /// Window's "Create Config" type picker (<see cref="Editor.ScriptableObjectCreator"/>), instead
    /// of the flat alphabetical list shown when no category is set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigCategoryAttribute : Attribute
    {
        public string Category { get; }

        public ConfigCategoryAttribute(string category)
        {
            Category = category;
        }
    }
}
