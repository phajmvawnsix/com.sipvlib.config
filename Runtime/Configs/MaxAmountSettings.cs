using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    [Serializable, Flags]
    public enum MaxAmountOption
    {
        None = 0,
        Lifetime = 1,
        Daily = 1 << 1,
        Weekly = 1 << 2,
        Monthly = 1 << 3,
        Yearly = 1 << 4,
        PerEvent = 1 << 5,
    }
    
    [Serializable]
#if ODIN_INSPECTOR
    [InfoBox("Configure maximum amount limits based on different time periods. Select which time constraints to apply.")]
#endif
    public class MaxAmountSettings
    {
        [SerializeField]
        [Tooltip("Select which time-based limits to apply to this max amount setting.")]
#if ODIN_INSPECTOR
        [InfoBox("Use flags to combine multiple time constraints (e.g., Daily | Weekly)", InfoMessageType.Info)]
#endif
        protected MaxAmountOption _maxAmountOption;

        [SerializeField]
        [Tooltip("Maximum amount allowed during the lifetime of the user.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasLifetimeFlag))]
#endif
        private long _lifeTime;

        [SerializeField]
        [Tooltip("Maximum amount allowed per day.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasDailyFlag))]
#endif
        private long _daily;

        [SerializeField]
        [Tooltip("Maximum amount allowed per week.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasWeeklyFlag))]
#endif
        private long _weekly;

        [SerializeField]
        [Tooltip("Maximum amount allowed per month.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasMonthlyFlag))]
#endif
        private long _monthly;

        [SerializeField]
        [Tooltip("Maximum amount allowed per year.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasYearlyFlag))]
#endif
        private long _yearly;

        [SerializeField]
        [Tooltip("Name of the event to limit the maximum amount per occurrence.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(HasPerEventFlag))]
#endif
        private string _eventName;

        #region Properties

        // Helper methods for ShowIf conditions
        private bool HasLifetimeFlag() => (_maxAmountOption & MaxAmountOption.Lifetime) != MaxAmountOption.None;
        private bool HasDailyFlag() => (_maxAmountOption & MaxAmountOption.Daily) != MaxAmountOption.None;
        private bool HasWeeklyFlag() => (_maxAmountOption & MaxAmountOption.Weekly) != MaxAmountOption.None;
        private bool HasMonthlyFlag() => (_maxAmountOption & MaxAmountOption.Monthly) != MaxAmountOption.None;
        private bool HasYearlyFlag() => (_maxAmountOption & MaxAmountOption.Yearly) != MaxAmountOption.None;
        private bool HasPerEventFlag() => (_maxAmountOption & MaxAmountOption.PerEvent) != MaxAmountOption.None;

        // Expose properties
        public MaxAmountOption MaxAmountOption => _maxAmountOption;
        public long LifeTime => _lifeTime;
        public long Daily => _daily;
        public long Weekly => _weekly;
        public long Monthly => _monthly;
        public long Yearly => _yearly;
        public string EventName => _eventName;
        #endregion
    }
}