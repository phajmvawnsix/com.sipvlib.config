using System;

namespace SiPVLib.Config.Compare
{
    [Serializable]
    public enum CompareMode
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual,
        GreaterThanOrEqual,
        GreaterThan,
    }
}