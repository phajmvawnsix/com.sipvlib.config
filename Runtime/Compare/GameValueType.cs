using System;

namespace SiPVLib.Config.Compare
{
    [Serializable]
    public enum GameValueType
    {
        /// <summary>
        /// Integer type, uses long as the value type.
        /// </summary>
        Integer = 0,
        /// <summary>
        /// Floating point type, uses decimal as the value type.
        /// </summary>
        FloatingPoint = 1,
        String,
        Bool,
        Structured = int.MaxValue,
    }
}