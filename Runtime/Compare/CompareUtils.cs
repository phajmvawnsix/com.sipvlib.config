namespace SiPVLib.Config.Compare
{
    public static class CompareUtils
    {
        public static bool Compare(this long a, long b, CompareMode mode)
        {
            return mode switch
            {
                CompareMode.Equal => a == b,
                CompareMode.NotEqual => a != b,
                CompareMode.GreaterThan => a > b,
                CompareMode.GreaterThanOrEqual => a >= b,
                CompareMode.LessThan => a < b,
                CompareMode.LessThanOrEqual => a <= b,
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static bool Compare(this int a, int b, CompareMode mode)
        {
            return mode switch
            {
                CompareMode.Equal => a == b,
                CompareMode.NotEqual => a != b,
                CompareMode.GreaterThan => a > b,
                CompareMode.GreaterThanOrEqual => a >= b,
                CompareMode.LessThan => a < b,
                CompareMode.LessThanOrEqual => a <= b,
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static bool Compare(this decimal a, decimal b, CompareMode mode)
        {
            return mode switch
            {
                CompareMode.Equal => a == b,
                CompareMode.NotEqual => a != b,
                CompareMode.GreaterThan => a > b,
                CompareMode.GreaterThanOrEqual => a >= b,
                CompareMode.LessThan => a < b,
                CompareMode.LessThanOrEqual => a <= b,
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
        
        public static bool Compare(this double a, double b, CompareMode mode)
        {
            return mode switch
            {
                CompareMode.Equal => a == b,
                CompareMode.NotEqual => a != b,
                CompareMode.GreaterThan => a > b,
                CompareMode.GreaterThanOrEqual => a >= b,
                CompareMode.LessThan => a < b,
                CompareMode.LessThanOrEqual => a <= b,
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static bool Compare(this float a, float b, CompareMode mode)
        {
            return mode switch
            {
                CompareMode.Equal => a == b,
                CompareMode.NotEqual => a != b,
                CompareMode.GreaterThan => a > b,
                CompareMode.GreaterThanOrEqual => a >= b,
                CompareMode.LessThan => a < b,
                CompareMode.LessThanOrEqual => a <= b,
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}